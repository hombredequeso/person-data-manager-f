open System
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

open Serialization

open Newtonsoft.Json
open Newtonsoft.Json.Linq

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

type PersonGetResponse = {
    id: Guid
    name: string
}

let toPersonGetResponse (getPersonResult: JObject): PersonGetResponse =
    {
        name = getPersonResult.["_source"].["name"].Value<String>()
        id = Guid.Parse(getPersonResult.["_id"].Value<string>())
    }

type ErrorResponse = {
    error: string
    details: string
}


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

let onFailure (errors: string list) (c: HttpContext) : Async<HttpContext option> = 
    let response = {
        error = "Server Error"
        details = String.Join(". ", errors)
    }
    (JSON ServerErrors.INTERNAL_ERROR response)(c)

let indexPerson (person: JObject) (c: HttpContext) : Async<HttpContext option> =
    async {
        let! dbResult = PersonDal.indexPerson person true
        let result = (OK "")
        return! result(c)
    }

let postPerson (request: HttpRequest): WebPart = 
    request.rawForm 
    |> deserialize<PersonPostBody>
    |> Hdq.Rop.map toEsEntity
    |> Hdq.Rop.either indexPerson onFailure
           
let passThroughWebPart (c: HttpContext) : Async<HttpContext Option> =
    async {
        return None
    }

let getPerson (guidId: Guid) (c: HttpContext): Async<HttpContext option> = 
    let successResponse e = e |> toPersonGetResponse |> JSON OK

    async {
            let! getPersonResult = PersonDal.getPerson guidId
            let x = Hdq.Rop.either successResponse onFailure getPersonResult
            return! x(c)
    }

open TryParser

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
