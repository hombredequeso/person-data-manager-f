module ApiDomain

open System
open Serialization
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
open Suave
open Suave.Successful

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
