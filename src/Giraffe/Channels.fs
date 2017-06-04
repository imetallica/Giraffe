module Giraffe.Channels

open Microsoft.AspNetCore.Http
open Giraffe.HttpHandlers
open Giraffe.HttpContextExtensions

type Channel = {
    Topic : string
    Payload : string
}

type ChannelHandler = string -> HttpHandler

(** 
let channel (topic : string) (f : HttpHandler) = 
    fun (ctx : HttpContext) ->
        async {
            if ctx.WebSockets.IsWebSocketRequest then
                return! f ctx
            else
                return None
        }

//let private channelParser (chl : ChannelHandler list) = 
//    List.map (fun x -> ) chl

let channels (path : string) (chl : ChannelHandler list) =
    fun (ctx : HttpContext) ->
        if ctx.WebSockets.IsWebSocketRequest
        then route path >=> choose (channelParser chl)
        else None
        |> async.Return
*)