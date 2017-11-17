open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

open Newtonsoft.Json
open Newtonsoft.Json.Serialization

// ========================================================================
// Utils:
open System.Threading.Tasks

let toAsync (t: Task<'a>) : Async<'a> = 
    Async.AwaitTask(t)

let JSON (responseCode: string -> WebPart) entity : WebPart =
    let settings = new JsonSerializerSettings()
    settings.ContractResolver <-
        new CamelCasePropertyNamesContractResolver()
    JsonConvert.SerializeObject(entity, settings)
    |> responseCode 
    >=> Writers.setMimeType "application/json; charset=utf-8"

// ========================================================================
// Db access:
open Nest

let elasticSearchClient = new ElasticClient()

let getEsHealth (client: ElasticClient) : Async<IClusterHealthResponse>  =
    client.ClusterHealthAsync() |> toAsync
    

// ========================================================================
// Web Api
type WebServiceHealthResponse = {
    Version: string
    }

type DbHealth = {
    Status: string
}

let toResponseBody (c: IClusterHealthResponse) : DbHealth =
    {Status = c.Status}

let getApiEsHealth : ElasticClient -> Async<DbHealth> = 
        getEsHealth >> Hdq.Async.map toResponseBody

let getDbHealth2 (ec: ElasticClient) (c: HttpContext) : Async<HttpContext option> =
    async {
        let! z = getApiEsHealth(ec)
        let m = JSON OK z
        return! m(c)
    }

[<EntryPoint>]
let main argv = 
    let app = choose [   
                GET >=> path "/health" >=> JSON Successful.OK {Version = "testing"}
                GET >=> path "/health/db" >=> getDbHealth2(elasticSearchClient)
    ]

    startWebServer defaultConfig app
    0 // return an integer exit code


