module ElasticSearchDb

open Nest
open Hdq.Async

let elasticSearchClient = new ElasticClient()

let getEsHealth (client: ElasticClient) : Async<IClusterHealthResponse>  =
    client.ClusterHealthAsync() |> toAsync
