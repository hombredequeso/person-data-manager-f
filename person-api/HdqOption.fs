﻿module HdqOption

let getOrElse def o =
    match o with
    | Some x -> x
    | None -> def

let apply fOpt xOpt = 
    match fOpt,xOpt with
    | Some f, Some x -> Some (f x)
    | _ -> None

let (<*>) = apply
let (<!>) = Option.map

let addSomeToList (s: 'a list) (e: 'a option) : 'a list = 
    match e with
    | None -> s
    | Some x -> x::s


let NullableToOption (n : System.Nullable<_>) = 
   if n.HasValue 
   then Some n.Value 
   else None