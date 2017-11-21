module PersonDal

open System
open ElasticSearchDb
open Newtonsoft.Json.Linq
open Elasticsearch.Net

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

let indexPerson (person: JObject) (refresh: bool): Async<DalResult> =

    let getProperty (o: JObject) (propertyName: string) : JProperty Option =
        let p = o.Property(propertyName)
        if (p = null) then None else Some p

    let selector = fun (s: IndexRequestParameters) -> 
                        s.Refresh (toRefresh refresh)
    let id: string = getProperty person "id" |> Option.fold (fun s t -> (string)t.Value) ""
    async {
        let taskResult = elasticSearchClient.LowLevel.IndexAsync<byte[]>(
                            personIndexName, 
                            personTypeName, 
                            Guid.NewGuid().ToString(), 
                            new PostData<Object>(toBytes person), 
                            fun (s: IndexRequestParameters) -> (s.Refresh (toRefresh refresh)))
        let! result = Hdq.Async.toAsync taskResult
        let responseCode = HdqOption.NullableToOption result.HttpStatusCode
        return match result.Success with
                | true -> DalResult.Ok
                | false -> 
                    let errorMessage = Option.fold (fun s i -> sprintf "Server response: %d" i) "No Response From db server" responseCode
                    DalResult.Failure errorMessage
    }

    