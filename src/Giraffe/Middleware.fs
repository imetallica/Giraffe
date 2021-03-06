module Giraffe.Middleware

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.FileProviders
open Microsoft.AspNetCore.Mvc.Razor
open Giraffe.HttpHandlers

/// ---------------------------
/// Logging helper functions
/// ---------------------------

let private getRequestInfo (ctx : HttpContext) =
    (ctx.Request.Protocol,
     ctx.Request.Method,
     ctx.Request.Path.ToString())
    |||> sprintf "%s %s %s"

/// ---------------------------
/// Default middleware
/// ---------------------------

type GiraffeMiddleware (next          : RequestDelegate,
                        handler       : HttpHandler,
                        loggerFactory : ILoggerFactory) =
    
    do if isNull next then raise (ArgumentNullException("next"))

    member __.Invoke (ctx : HttpContext) =
        async {
            let! result = handler ctx

            let logger = loggerFactory.CreateLogger<GiraffeMiddleware>()
            if logger.IsEnabled LogLevel.Debug then
                match result with
                | Some _ -> sprintf "Giraffe returned Some for %s" (getRequestInfo ctx)
                | None   -> sprintf "Giraffe returned None for %s" (getRequestInfo ctx)
                |> logger.LogDebug

            if (result.IsNone) then
                return!
                    next.Invoke ctx
                    |> Async.AwaitTask
        } |> Async.StartAsTask

/// ---------------------------
/// Error Handling middleware
/// ---------------------------

type GiraffeErrorHandlerMiddleware (next          : RequestDelegate,
                                    errorHandler  : ErrorHandler,
                                    loggerFactory : ILoggerFactory) =

    do if isNull next then raise (ArgumentNullException("next"))

    member __.Invoke (ctx : HttpContext) =
        async {
            let logger = loggerFactory.CreateLogger<GiraffeErrorHandlerMiddleware>()
            try
                return!
                    next.Invoke ctx
                    |> Async.AwaitTask
            with ex ->
                try
                    return!
                        errorHandler ex logger ctx
                        |> Async.Ignore
                with ex2 ->
                    logger.LogError(EventId(0), ex,  "An unhandled exception has occurred while executing the request.")
                    logger.LogError(EventId(0), ex2, "An exception was thrown attempting to handle the original exception.")
        } |> Async.StartAsTask

/// ---------------------------
/// Extension methods for convenience
/// ---------------------------

type IApplicationBuilder with
    member this.UseGiraffe (handler : HttpHandler) =
        this.UseMiddleware<GiraffeMiddleware>(handler)
        |> ignore

    member this.UseGiraffeErrorHandler (handler : ErrorHandler) =
        this.UseMiddleware<GiraffeErrorHandlerMiddleware>(handler)
        |> ignore

type IServiceCollection with
    member this.AddRazorEngine (viewsFolderPath : string) =
        this.Configure<RazorViewEngineOptions>(
            fun options ->
                options.FileProviders.Clear()
                options.FileProviders.Add(new PhysicalFileProvider(viewsFolderPath)))
            .AddMvc()
        |> ignore