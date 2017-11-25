namespace Hdq

module TryParser =
    // http://www.fssnip.net/2y/title/Functional-wrappers-for-TryParse-APIs

    let tryParseWith tryParseFunc = tryParseFunc >> function
        | true, v    -> Some v
        | false, _   -> None

    let parseDate   = tryParseWith System.DateTime.TryParse
    let parseInt    = tryParseWith System.Int32.TryParse
    let parseInt64    = tryParseWith System.Int64.TryParse
    let parseSingle = tryParseWith System.Single.TryParse
    let parseDouble = tryParseWith System.Double.TryParse
    let parseUInt32 = tryParseWith System.UInt32.TryParse
    let parseGuid = tryParseWith System.Guid.TryParse

    let (|Date|_|)   = parseDate
    let (|Int|_|)    = parseInt
    let (|Int64|_|)    = parseInt64
    let (|Single|_|) = parseSingle
    let (|Double|_|) = parseDouble
    let (|Guid|_|) = parseGuid
