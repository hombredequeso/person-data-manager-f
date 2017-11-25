module ApiDomain


type WebServiceHealthResponse = {
    Version: string
    }

type DbHealth = {
    Status: string
}

type ErrorResponse = {
    error: string
    details: string list
}

open ElasticSearchDb
open Nest
open SuaveSerializer
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
