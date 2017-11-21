open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open Newtonsoft.Json.Linq

// ========================================================================
// Utils:

let JSON (responseCode: string -> WebPart) entity : WebPart =
    let settings = new JsonSerializerSettings()
    settings.ContractResolver <-
        new CamelCasePropertyNamesContractResolver()
    JsonConvert.SerializeObject(entity, settings)
    |> responseCode 
    >=> Writers.setMimeType "application/json; charset=utf-8"

let (.=) key (value : obj) = new JProperty(key, value)
let jobj jProperties =
    let jObject = new JObject()
    jProperties |> List.iter jObject.Add
    jObject
let jArray jObjects =
    let jArray = new JArray()
    jObjects |> List.iter jArray.Add
    jArray
    

// ========================================================================
// Web Api
type WebServiceHealthResponse = {
    Version: string
    }

type DbHealth = {
    Status: string
}

type PersonPostBody = {
    name: string
}

let toEsEntity (person: PersonPostBody) : JObject =
    jobj    [
        "name" .= person.name
    ]



open ElasticSearchDb
open Nest

let toResponseBody (c: IClusterHealthResponse) : DbHealth =
    {Status = c.Status}

let getApiEsHealth : ElasticClient -> Async<DbHealth> = 
        getEsHealth >> Hdq.Async.map toResponseBody

let getDbHealth (ec: ElasticClient) (c: HttpContext) : Async<HttpContext option> =
    async {
        let! z = getApiEsHealth(ec)
        let m = JSON OK z
        return! m(c)
    }

// open Hdq.Rop

// let tryCatch f x =
//     try
//         f x |> succeed
//     with
//     | ex -> fail ex.Message

let postPerson (request: HttpRequest) (c: HttpContext) : Async<HttpContext option> =
    let reqBody = request.rawForm |> System.Text.Encoding.UTF8.GetString |> JsonConvert.DeserializeObject<PersonPostBody>
    let esEntity = toEsEntity reqBody
    async {
        let! dbResult = PersonDal.indexPerson esEntity true
        let result = (OK "")
        return! result(c)
    }


[<EntryPoint>]
let main argv = 
    let app = choose [   
                GET >=> path "/health" >=> JSON Successful.OK {Version = "testing"}
                GET >=> path "/health/db" >=> getDbHealth(elasticSearchClient)
                path "/api/person" >=> POST >=> request(fun r -> postPerson(r))
    ]

    startWebServer defaultConfig app
    0 // return an integer exit code
