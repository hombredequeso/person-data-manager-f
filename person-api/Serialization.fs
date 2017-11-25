module Serialization

open Newtonsoft.Json
open Newtonsoft.Json.Linq

open Hdq.Rop
open Hdq.Option


let (.=) key (value : obj) = new JProperty(key, value)

let jobj jProperties =
    let jObject = new JObject()
    jProperties |> List.iter jObject.Add
    jObject

let jArray jObjects =
    let jArray = new JArray()
    jObjects |> List.iter jArray.Add
    jArray

let getProperty (propertyName: string) (obj: JObject) : JToken option =
    let prop = obj.GetValue propertyName
    valueToOption prop

let deserialize<'a> byteArray = byteArray
                                |> System.Text.Encoding.UTF8.GetString 
                                |> tryCatch JsonConvert.DeserializeObject<'a>

let jObjFromBytes (byteArray: byte[]) : RopResult<JObject, string> = 
    byteArray
    |> System.Text.Encoding.UTF8.GetString 
    |> tryCatch JObject.Parse

let asJObject (o: JToken) =
    match o with
    | :? JObject -> o :?> JObject |> Some
    | _ -> None

