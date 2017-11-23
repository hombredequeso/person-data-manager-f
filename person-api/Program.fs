open System
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

open Newtonsoft.Json.Linq

open Hdq.Rop
open TryParser
open Serialization
open ElasticSearchDb
open ApiDomain

let onFailure (errors: string list) (c: HttpContext) : Async<HttpContext option> = 
    let response = {
        error = "Server Error"
        details = String.Join(". ", errors)
    }
    (JSON ServerErrors.INTERNAL_ERROR response)(c)

let indexPerson (person: JObject) (c: HttpContext) : Async<HttpContext option> =
    async {
        let! dbResult = PersonDal.indexPerson elasticSearchClient.LowLevel person true
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
            let! getPersonResult = PersonDal.getPerson elasticSearchClient.LowLevel guidId
            let x = Hdq.Rop.either successResponse onFailure getPersonResult
            return! x(c)
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
