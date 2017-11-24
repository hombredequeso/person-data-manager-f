module PersonDal

open System
open Newtonsoft.Json.Linq
open Newtonsoft.Json
open Elasticsearch.Net
open Hdq.Rop

type Person = {
    id: Guid
    name: string
}

type ToPersonError =
    | PropertyDoesNotExist of string
    | InvalidGuidId of Guid
    | IdPropertyNotAValidGuidId of string

let validateExists (propertyName: string) (obj: JObject) = 
    match obj.TryGetValue propertyName with
    | true, x -> None
    | false, _ -> PropertyDoesNotExist propertyName |> Some

let propertyIsValidGuid  (propertyName: string) (obj: JObject) = 
    match obj.TryGetValue propertyName with
    | true, x ->
                match x.Value<string>() |> Guid.TryParse with
                | true, x ->
                    if (x = Guid.Empty) then (InvalidGuidId x |> Some ) else None
                | false, _ -> IdPropertyNotAValidGuidId (x.Value<string>()) |> Some
    | false, _ -> None  // validateExists should take care of this

let toPerson (obj : JObject ) : RopResult<Person, ToPersonError> =
    let validators = [validateExists "id"; validateExists "name"; propertyIsValidGuid "id"]
    let validationResult = validators |> List.map (fun v -> v obj)
    let r2 = List.fold 
                    (fun (s: ToPersonError list) (t: ToPersonError option) ->
                        match t with
                        | Some v -> v::s
                        | None -> s
                    ) 
                    [] 
                    validationResult
    match List.length r2 with
    | 0 ->
            {
                id = obj.["id"].Value<string>() |> Guid.Parse
                name = obj.["name"].Value<string>()
            } |> succeed
    | _ -> Failure r2

let toBytes (jObject: JObject): byte[] = 
    jObject.ToString() |> System.Text.Encoding.ASCII.GetBytes

let toRefresh (refresh: bool) : Refresh =
    if refresh then Refresh.True else Refresh.False

let toFunc fSharpFunc : Func<IndexRequestParameters,IndexRequestParameters> =
    new Func<IndexRequestParameters,IndexRequestParameters>(fSharpFunc)

let personIndexName = "person"
let personTypeName = "person"

type DalResult =
    | Ok
    | Failure of string

let getPerson (elasticSearchClient: IElasticLowLevelClient) (id: Guid) : Async<RopResult<JObject, string>> =
    async {
        let taskResult = elasticSearchClient.GetAsync<byte[]>("person", "person", id.ToString())
        let! result = Hdq.Async.toAsync taskResult
        let responseCode = HdqOption.NullableToOption result.HttpStatusCode
        return match result.Success with
                | true ->
                    let serializedBody = System.Text.Encoding.Default.GetString(result.Body);
                    let result = tryCatch JObject.Parse serializedBody
                    either succeed Hdq.Rop.Failure result
                | false -> 
                    let errorMessage = Option.fold (fun s i -> sprintf "Server response: %d" i) "No Response From db server" responseCode
                    fail errorMessage
    }

let indexPerson (elasticSearchClient: IElasticLowLevelClient) (person: Person) (refresh: bool): Async<DalResult> =

    let selector = fun (s: IndexRequestParameters) -> 
                        s.Refresh (toRefresh refresh)
    async {
        let serializedPerson = JsonConvert.SerializeObject person
        let taskResult = elasticSearchClient.IndexAsync<byte[]>(
                            personIndexName, 
                            personTypeName, 
                            person.id.ToString(),
                            new PostData<Object>(serializedPerson), 
                            fun (s: IndexRequestParameters) -> (s.Refresh (toRefresh refresh)))
        let! result = Hdq.Async.toAsync taskResult
        let responseCode = HdqOption.NullableToOption result.HttpStatusCode
        return match result.Success with
                | true -> DalResult.Ok
                | false -> 
                    let errorMessage = Option.fold (fun s i -> sprintf "Server response: %d" i) "No Response From db server" responseCode
                    DalResult.Failure errorMessage
    }

    