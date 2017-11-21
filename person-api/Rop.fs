namespace Hdq

module Rop =

    /// A Result is a success or failure
    /// The Success case has a success value, plus a list of messages
    /// The Failure case has just a list of messages
    type RopResult<'TSuccess, 'TMessage> =
        | Success of 'TSuccess * 'TMessage list
        | Failure of 'TMessage list

    /// create a Success with no messages
    let succeed x =
        Success (x,[])


    /// create a Success with a message
    let succeedWithMsg x msg =
        Success (x,[msg])

    /// create a Failure with a message
    let fail msg =
        Failure [msg]

    /// A function that applies either fSuccess or fFailure 
    /// depending on the case.
    let either fSuccess fFailure = function
        | Success (x,msgs) -> fSuccess (x,msgs) 
        | Failure errors -> fFailure errors 

    // is the same as...
    // let either2 fSuccess fFailure ropResult = 
    //     match ropResult with
    //     | Success (x,msgs) -> fSuccess (x,msgs) 
    //     | Failure errors -> fFailure errors 

    let map f r =
        match r with
        | Success(x, msgs) -> Success(f(x), msgs)
        | Failure e -> Failure e

    /// merge messages with a result
    let mergeMessages msgs result =
        let fSuccess (x,msgs2) = 
            Success (x, msgs @ msgs2) 
        let fFailure errs = 
            Failure (errs @ msgs) 
        either fSuccess fFailure result

    /// given a function that generates a new RopResult
    /// apply it only if the result is on the Success branch
    /// merge any existing messages with the new result
    /// MC: often just called bind: http://fsharpforfunandprofit.com/posts/recipe-part2/
    let bind f result =
        let fSuccess (x,msgs) = 
            f x |> mergeMessages msgs
        let fFailure errs = 
            Failure errs 
        either fSuccess fFailure result

    /// given a function wrapped in a result
    /// and a value wrapped in a result
    /// apply the function to the value only if both are Success
    let apply f result =
        match f,result with
        | Success (f,msgs1), Success (x,msgs2) -> 
            (f x, msgs1@msgs2) |> Success 
        | Failure errs, Success (_,msgs) 
        | Success (_,msgs), Failure errs -> 
            errs @ msgs |> Failure
        | Failure errs1, Failure errs2 -> 
            errs1 @ errs2 |> Failure 

    /// infix version of apply
    let (<*>) = apply

    /// given a function that transforms a value
    /// apply it only if the result is on the Success branch
    /// ('a -> 'b) -> RopResult<'a, 'c> -> RopResult<'b, 'c>
    let lift f result =
        let f' =  f |> succeed
        apply f' result 

    /// given two values wrapped in results apply a function to both
    let lift2 f result1 result2 =
        let f' = lift f result1
        apply f' result2 

    /// given three values wrapped in results apply a function to all
    let lift3 f result1 result2 result3 =
        let f' = lift2 f result1 result2 
        apply f' result3

    /// given four values wrapped in results apply a function to all
    let lift4 f result1 result2 result3 result4 =
        let f' = lift3 f result1 result2 result3 
        apply f' result4

    /// infix version of liftR
    let (<!>) = lift


    /// synonym for liftR
    let mapR = lift

    // convert a dead-end function into a one-track function
    let tee f x = 
        f x; x 

    // convert a function returning a value into a unit function
    let ignoref f x =
        f x |> ignore

    /// given an RopResult, call a unit function on the success branch
    /// and pass thru the result
    let successTee f result = 
        let fSuccess (x,msgs) = 
            f (x,msgs)
            Success (x,msgs) 
        let fFailure errs = Failure errs 
        either fSuccess fFailure result

    let successTee' f result = 
        let fSuccess (x,msgs) = 
            f x
            Success (x,msgs) 
        let fFailure errs = Failure errs 
        either fSuccess fFailure result

    /// given an RopResult, call a unit function on the failure branch
    /// and pass thru the result
    let failureTee f result = 
        let fSuccess (x,msgs) = Success (x,msgs) 
        let fFailure errs = 
            f errs
            Failure errs 
        either fSuccess fFailure result

    /// given an RopResult, map the messages to a different error type
    let mapMessagesR f result = 
        match result with 
        | Success (x,msgs) -> 
            let msgs' = List.map f msgs
            Success (x, msgs')
        | Failure errors -> 
            let errors' = List.map f errors 
            Failure errors' 

    /// given an RopResult, in the success case, return the value.
    /// In the failure case, determine the value to return by 
    /// applying a function to the errors in the failure case
    let valueOrDefault f result = 
        match result with 
        | Success (x,_) -> x
        | Failure errors -> f errors

    /// lift an option to a RopResult.
    /// Return Success if Some
    /// or the given message if None
    let failIfNone message = function
        | Some x -> succeed x
        | None -> fail message 

    /// given an RopResult option, return it
    /// or the given message if None
    let failIfNoneR message = function
        | Some rop -> rop
        | None -> fail message 


    /// MC: Operator to express bindR added by MC: uses the convention that bind is >>=
    /// given a function that generates a new RopResult
    /// apply it only if the result is on the Success branch
    /// merge any existing messages with the new result
    let (>>=) twoTrackInput switchFunction =  bind switchFunction twoTrackInput

    /// Turn a one track function f into a two track function
    /// Enables putting a one track function into a two track pipeline.
    let switch f x =
        f x |> succeed

    /// Restrict usage to tests and mutually agreed areas of functionality (e.g. DAL only)
    let deRail x =
        match x with
            | Success(e, _) -> e
            | _ -> failwith "Failure ROP value: only Success allowed"

    // Not needed, it already exists.
    // let optionToRopResult failMessage o =
    //     match o with
    //         | Some x -> succeed x
    //         | None -> fail failMessage


    type RopBuilder() =
        member this.Bind(x, f) = 
            bind f x

        member this.Return(x) = 
            succeed x

        member this.ReturnFrom(x) =
            x

        // Zero: for a workflow that has no return value, create a return value that effectively means "no return value"
        //       That would be a rop result that succeeded with a unit value.
        member this.Zero() =
            succeed ()

    let rop = new RopBuilder()
