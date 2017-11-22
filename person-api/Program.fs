open System
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

type PersonGetResponse = {
    id: Guid
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

open Hdq.Rop


let innerPost (person: JObject) =
    async {
        let! dbResult = PersonDal.indexPerson person true
        let result = (OK "")
        return result
    }

let onFailure errors : Async<WebPart> =
    async {
       return (ServerErrors.INTERNAL_ERROR "General catch all: all is not well...")
    }

let deserialize<'a> byteArray = byteArray
                                |> System.Text.Encoding.UTF8.GetString 
                                |> tryCatch JsonConvert.DeserializeObject<'a>

let postPerson (request: HttpRequest) (c: HttpContext) : Async<HttpContext option> =
    async {
        let! esEntity = request.rawForm 
                        |> deserialize<PersonPostBody>
                        |> Hdq.Rop.map toEsEntity
                        |> Hdq.Rop.either innerPost onFailure
        return! esEntity(c)
    }

open TryParser
open PersonDal
open System

let passThroughWebPart (c: HttpContext) : Async<HttpContext Option> =
    async {
        return None
    }

let toPersonGetResponse (getPersonResult: JObject): PersonGetResponse =
    {
        name = getPersonResult.["_source"].["name"].Value<String>()
        id = Guid.Parse(getPersonResult.["_id"].Value<string>())
    }

let getPerson (guidId: Guid) (c: HttpContext): Async<HttpContext option> = 
    async {
            let! getPersonResult = PersonDal.getPerson guidId
            match getPersonResult with
            | Hdq.Rop.Failure s -> 
                return! ((ServerErrors.INTERNAL_ERROR "General catch all: all is not well...")(c))
            | Success e ->
                return! (e |> toPersonGetResponse |> JSON OK)(c)
    }

[<EntryPoint>]
let main argv = 
    let app = choose [   
                GET >=> path "/health" >=> JSON Successful.OK {Version = "testing"}
                GET >=> path "/health/db" >=> getDbHealth(elasticSearchClient)
                path "/api/person" >=> POST >=> request(fun r -> postPerson(r))
                pathScan "/api/person/%s"  
                    (fun id -> Option.fold 
                                    (fun _ guidId -> (getPerson guidId)) 
                                    passThroughWebPart
                                    (parseGuid id))
    ]

    startWebServer defaultConfig app
    0 // return an integer exit code
