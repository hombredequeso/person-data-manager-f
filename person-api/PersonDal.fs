module PersonDal

open System
open Newtonsoft.Json.Linq
open Elasticsearch.Net
open Hdq.Rop
open Hdq.ElasticsearchApi

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


let personIndexName = "person"
let personTypeName = "person"

let getPerson (elasticSearchClient: IElasticLowLevelClient) (id: Guid) 
        : Async<RopResult<JObject, GetEntityError>> =
    getEntity elasticSearchClient personIndexName personTypeName id

let indexPerson (elasticSearchClient: IElasticLowLevelClient) (person: Person) (refresh: bool)
        : Async<RopResult<unit, string>> =
    indexEntity 
        elasticSearchClient 
        personIndexName 
        personTypeName 
        (fun p -> p.id.ToString()) 
        person 
        refresh
