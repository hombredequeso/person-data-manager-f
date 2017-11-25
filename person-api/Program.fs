open System
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

open Hdq.Rop
open Hdq.TryParser
open Serialization
open ElasticSearchDb
open ApiDomain

open PersonDal
open SuaveSerializer
open Hdq.ElasticsearchApi

type PostPersonError =
    | BodyIsInvalidJson of string
    | InvalidPerson of PersonDal.ToPersonError

let indexPerson (person: PersonDal.Person) (c: HttpContext) : Async<HttpContext option> =
    async {
        let! dbResult = PersonDal.indexPerson elasticSearchClient.LowLevel person true
        let result = Hdq.Rop.either 
                        (fun _ -> JSON CREATED  person) 
                        (fun e -> 
                            let response = {
                                error = "Server Error"
                                details = e
                            }
                            JSON ServerErrors.INTERNAL_ERROR response) 
                        dbResult
        return! result(c)
    }

let toApiErrorMessagePersonError = function
    | PropertyDoesNotExist p -> sprintf "Property does not exist: %s" p
    | InvalidGuidId p -> sprintf "Invalid Guid id: %s" (p.ToString())
    | IdPropertyNotAValidGuidId p -> sprintf "Id Property is not a guid: %s" p

let toApiErrorMessage = function
    | BodyIsInvalidJson _ -> "Body is not valid JSON"
    | InvalidPerson e -> toApiErrorMessagePersonError e

let onPostFailure (errors: PostPersonError list) (c: HttpContext) : Async<HttpContext option> = 
    let response = {
        error = "Server Error"
        details = errors |> List.map toApiErrorMessage
    }
    (JSON ServerErrors.INTERNAL_ERROR response)(c)

let postPerson (request: HttpRequest): WebPart = 
    let jObj = jObjFromBytes >> mapMessagesR PostPersonError.BodyIsInvalidJson
    let toPerson = PersonDal.toPerson >> mapMessagesR PostPersonError.InvalidPerson

    request.rawForm 
    |> jObj
    >>= toPerson
    |> Hdq.Rop.either indexPerson onPostFailure
           

let getResponseErrorMessage = function
    | DbResultHasNoSourceProperty -> "DbResultHasNoSourceProperty"
    | DbResultCannotBeDeserialized s -> sprintf "DbResultCannotBeDeserialized: %s" s
    | DbServerError s ->  sprintf "DbServerError: %s" s

let onGetFailure (errors: GetEntityError list) (c: HttpContext) : Async<HttpContext option> = 
    let response = {
        error = "Server Error"
        details = errors |> List.map getResponseErrorMessage
    }
    (JSON ServerErrors.INTERNAL_ERROR response)(c)

let onGetSuccess (e:'a) : WebPart = 
    OK (e.ToString()) >=> Writers.setMimeType "application/json; charset=utf-8"

let getPerson (guidId: Guid) (c: HttpContext): Async<HttpContext option> = 
    async {
            let! getPersonResult = PersonDal.getPerson elasticSearchClient.LowLevel guidId
            let x = Hdq.Rop.either onGetSuccess onGetFailure getPersonResult
            return! x(c)
    }

let passThroughWebPart (c: HttpContext) : Async<HttpContext Option> =
    async {
        return None
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
