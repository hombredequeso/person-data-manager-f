namespace Hdq

module ElasticsearchApi = 

    open System
    open Newtonsoft.Json.Linq
    open Newtonsoft.Json
    open Elasticsearch.Net
    open Hdq.Rop

    let toRefresh (refresh: bool) : Refresh =
        if refresh then Refresh.True else Refresh.False
        
    type GetEntityError =
        | DbResultHasNoSourceProperty
        | DbResultCannotBeDeserialized of string
        | DbServerError of string

    let getEntity (elasticSearchClient: IElasticLowLevelClient) (indexName: string) (typeName: string) (id: Guid) 
            : Async<RopResult<JObject, GetEntityError>> =

        let getSourceObject =
            Serialization.getProperty "_source" 
            >> Option.bind Serialization.asJObject 
            >> failIfNone DbResultHasNoSourceProperty

        let bytesToJObject =
            System.Text.Encoding.Default.GetString
            >> tryCatch JObject.Parse
            >> mapMessagesR DbResultCannotBeDeserialized

        async {
            let taskResult = elasticSearchClient.GetAsync<byte[]>(indexName, typeName, id.ToString())
            let! result = Hdq.Async.toAsync taskResult
            let responseCode = Hdq.Option.nullableToOption result.HttpStatusCode
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
            let responseCode = Hdq.Option.nullableToOption result.HttpStatusCode
            return match result.Success with
                    | true -> () |> succeed
                    | false -> 
                        let errorMessage = Option.fold (fun s i -> sprintf "Server response: %d" i) "No Response From db server" responseCode
                        errorMessage |> fail
        }
