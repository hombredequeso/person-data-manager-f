module SuaveSerializer

open Suave
open Suave.Operators
open Newtonsoft.Json
open Newtonsoft.Json.Serialization

let JSON (responseCode: string -> WebPart) entity : WebPart =
    let settings = new JsonSerializerSettings()
    settings.ContractResolver <-
        new CamelCasePropertyNamesContractResolver()
    JsonConvert.SerializeObject(entity, settings)
    |> responseCode 
    >=> Writers.setMimeType "application/json; charset=utf-8"
