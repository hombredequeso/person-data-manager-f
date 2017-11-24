module Serialization

open Suave
open Suave.Operators

open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open Newtonsoft.Json.Linq

open Hdq.Rop

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

let deserialize<'a> byteArray = byteArray
                                |> System.Text.Encoding.UTF8.GetString 
                                |> tryCatch JsonConvert.DeserializeObject<'a>

let toJObject byteArray = byteArray
                                |> System.Text.Encoding.UTF8.GetString 
                                |> tryCatch JObject.Parse