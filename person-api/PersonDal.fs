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

let validateIsGuid (propertyName: string) (obj: JObject) = 
    match obj.TryGetValue propertyName with
    | true, x ->
                match x.Value<string>() |> Guid.TryParse with
                | true, x ->
                    if (x = Guid.Empty) then (InvalidGuidId x |> Some ) else None
                | false, _ -> IdPropertyNotAValidGuidId (x.Value<string>()) |> Some
    | false, _ -> None  // validateExists should take care of this

let toPerson (obj : JObject ) : RopResult<Person, ToPersonError> =
    let validators = [validateExists "id"; validateExists "name"; validateIsGuid "id"]
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

let toRefresh (refresh: bool) : Refresh =
    if refresh then Refresh.True else Refresh.False

let personIndexName = "person"
let personTypeName = "person"

type GetPersonError =
    | DbResultHasNoSourceProperty
    | DbResultCannotBeDeserialized of string
    | DbServerError of string

let getPerson (elasticSearchClient: IElasticLowLevelClient) (id: Guid) 
        : Async<RopResult<JObject, GetPersonError>> =

    let getSourceObject =
        Serialization.getProperty "_source" 
        >> Option.bind Serialization.asJObject 
        >> failIfNone DbResultHasNoSourceProperty

    let bytesToJObject =
        System.Text.Encoding.Default.GetString
        >> tryCatch JObject.Parse
        >> mapMessagesR DbResultCannotBeDeserialized

    async {
        let taskResult = elasticSearchClient.GetAsync<byte[]>("person", "person", id.ToString())
        let! result = Hdq.Async.toAsync taskResult
        let responseCode = HdqOption.nullableToOption result.HttpStatusCode
        return match result.Success with
                | true ->
                    result.Body |> bytesToJObject >>= getSourceObject
                | false -> 
                    let errorMessage = Option.fold (fun s i -> sprintf "Server response: %d" i) "No Response From db server" responseCode
                    DbServerError errorMessage |> fail
    }

let indexEntity<'a> 
        (elasticSearchClient: IElasticLowLevelClient) 
        indexName 
        indexType 
        (getId: 'a -> string)
        (entity: 'a) 
        (refresh: bool)
        : Async<RopResult<unit, string>> =

    async {
        let selector = fun (s: IndexRequestParameters) -> 
                            s.Refresh (toRefresh refresh)
        let serializedPerson = JsonConvert.SerializeObject entity
        let taskResult = elasticSearchClient.IndexAsync<byte[]>(
                            indexName, 
                            indexType, 
                            getId entity,
                            new PostData<Object>(serializedPerson), 
                            fun (s: IndexRequestParameters) -> (s.Refresh (toRefresh refresh)))
        let! result = Hdq.Async.toAsync taskResult
        let responseCode = HdqOption.nullableToOption result.HttpStatusCode
        return match result.Success with
                | true -> () |> succeed
                | false -> 
                    let errorMessage = Option.fold (fun s i -> sprintf "Server response: %d" i) "No Response From db server" responseCode
                    errorMessage |> fail
    }

let indexPerson (elasticSearchClient: IElasticLowLevelClient) (person: Person) (refresh: bool)
        : Async<RopResult<unit, string>> =
    indexEntity 
        elasticSearchClient 
        personIndexName 
        personTypeName 
        (fun p -> p.id.ToString()) 
        person 
        refresh
