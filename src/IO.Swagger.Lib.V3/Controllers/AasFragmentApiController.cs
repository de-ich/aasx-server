
using AasSecurity.Exceptions;
using AasxServer;
using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using IO.Swagger.Attributes;
using IO.Swagger.Controllers;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.Services;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;

namespace IO.Swagger.Lib.V3.Controllers
{
    public class AasFragmentApiController : ControllerBase
    {
        private readonly IAppLogger<AasFragmentApiController> _logger;
        private readonly IAssetAdministrationShellService _aasService;
        private readonly IBase64UrlDecoderService _decoderService;
        private readonly IAuthorizationService _authorizationService;
        private readonly IFragmentObjectRetrievalService _fragmentRetrievalService;
        private readonly IFragmentObjectConverterService _fragmentConverterService;

        public AasFragmentApiController(IAppLogger<AasFragmentApiController> logger, IAssetAdministrationShellService aasService, IBase64UrlDecoderService decoderService, IAuthorizationService authorizationService, IFragmentObjectRetrievalService fragmentRetrievalService, IFragmentObjectConverterService fragmentConverterService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            _aasService = aasService ?? throw new ArgumentNullException(nameof(aasService));
            _decoderService = decoderService ?? throw new ArgumentNullException(nameof(decoderService));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _fragmentRetrievalService = fragmentRetrievalService ?? throw new ArgumentNullException(nameof(fragmentRetrievalService));
            _fragmentConverterService = fragmentConverterService ?? throw new ArgumentNullException(nameof(fragmentConverterService));
        }

        /// <summary>
        /// Returns a specific fragment from a File element in a submodel
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="idShortPath">IdShort path to the submodel element (dot-separated)</param>
        /// <param name="fragmentType">Fragment Type</param>
        /// <param name="content">Determines the type of content to be returned</param>
        /// <param name="level">Determines the structural depth of the respective resource content</param>
        /// <param name="extent">Determines to which extent the resource is being serialized</param>
        /// <response code="200">Requested submodel element</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/shells/{aasIdentifier}/submodels/{submodelIdentifier}/submodel-elements/{idShortPath}/fragmentTypes/{fragmentType}/${content}")]
        [ValidateModelState]
        [SwaggerOperation("GetRootFragment")]
        [SwaggerResponse(statusCode: 200, type: typeof(object), description: "Requested fragment object")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetRootFragmentContent([FromRoute][Required] string aasIdentifier, [FromRoute][Required] string submodelIdentifier, [FromRoute][Required] string idShortPath, [FromRoute][Required] string fragmentType, [FromRoute][Required] ContentEnum content, [FromQuery] LevelEnum level, [FromQuery] ExtentEnum extent)
        {
            return GetFragmentValue(aasIdentifier, submodelIdentifier, idShortPath, fragmentType, "", content, level, extent);
        }

        /// <summary>
        /// Returns a specific fragment from a File element in a submodel
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="idShortPath">IdShort path to the submodel element (dot-separated)</param>
        /// <param name="fragmentType">Fragment Type</param>
        /// <param name="fragment">Fragment</param>
        /// <param name="level">Determines the structural depth of the respective resource content</param>
        /// <param name="extent">Determines to which extent the resource is being serialized</param>
        /// <response code="200">Requested submodel element</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/shells/{aasIdentifier}/submodels/{submodelIdentifier}/submodel-elements/{idShortPath}/fragmentTypes/{fragmentType}/fragments/{fragment}")]
        [ValidateModelState]
        [SwaggerOperation("GetFragment")]
        [SwaggerResponse(statusCode: 200, type: typeof(object), description: "Requested fragment object")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetFragment([FromRoute][Required] string aasIdentifier, [FromRoute][Required] string submodelIdentifier, [FromRoute][Required] string idShortPath, [FromRoute][Required] string fragmentType, [FromRoute][Required]string fragment, [FromQuery] LevelEnum level, [FromQuery] ExtentEnum extent)
        {
            return GetFragmentValue(aasIdentifier, submodelIdentifier, idShortPath, fragmentType, fragment, ContentEnum.Normal, level, extent);
        }

        /// <summary>
        /// Returns a specific fragment from a File element in a submodel
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="idShortPath">IdShort path to the submodel element (dot-separated)</param>
        /// <param name="fragmentType">Fragment Type</param>
        /// <param name="fragment">Fragment</param>
        /// <param name="content">Determines the type of content to be returned</param>
        /// <param name="level">Determines the structural depth of the respective resource content</param>
        /// <param name="extent">Determines to which extent the resource is being serialized</param>
        /// <response code="200">Requested submodel element</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/shells/{aasIdentifier}/submodels/{submodelIdentifier}/submodel-elements/{idShortPath}/fragmentTypes/{fragmentType}/fragments/{fragment}/${content}")]
        [ValidateModelState]
        [SwaggerOperation("GetFragmentContent")]
        [SwaggerResponse(statusCode: 200, type: typeof(object), description: "Requested fragment object")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetFragmentContent([FromRoute][Required] string aasIdentifier, [FromRoute][Required] string submodelIdentifier, [FromRoute][Required] string idShortPath, [FromRoute][Required] string fragmentType, [FromRoute][Required] string fragment, [FromRoute][Required] ContentEnum content, [FromQuery] LevelEnum level, [FromQuery] ExtentEnum extent)
        {
            return GetFragmentValue(aasIdentifier, submodelIdentifier, idShortPath, fragmentType, fragment, content, level, extent);
        }

        private IActionResult GetFragmentValue(string aasIdentifier, string submodelIdentifier, string idShortPath, string fragmentType, string fragment, ContentEnum content, LevelEnum level, ExtentEnum extent)
        {
            var decodedAasIdentifier = _decoderService.Decode("aasIdentifier", aasIdentifier);
            var decodedSubmodelIdentifier = _decoderService.Decode("submodelIdentifier", submodelIdentifier);
            var decodedFragment = _decoderService.Decode("fragment", fragment) ?? ""; // an empty fragment means 'root object'

            _logger.LogInformation($"Received request to get fragment {fragment} of type {fragmentType} in the submodel element at {idShortPath} from the submodel with id {submodelIdentifier} and the AAS with id {aasIdentifier}.");

            if (!Program.noSecurity)
            {
                var submodel = _aasService.GetSubmodelById(decodedAasIdentifier, decodedSubmodelIdentifier);
                User.Claims.ToList().Add(new Claim("idShortPath", submodel.IdShort + "." + idShortPath));
                var claimsList = new List<Claim>(User.Claims)
                {
                    new Claim("IdShortPath", submodel.IdShort + "." + idShortPath)
                };
                var identity = new ClaimsIdentity(claimsList, "AasSecurityAuth");
                var principal = new System.Security.Principal.GenericPrincipal(identity, null);
                var authResult = _authorizationService.AuthorizeAsync(principal, submodel, "SecurityPolicy").Result;
                if (!authResult.Succeeded)
                {
                    throw new NotAllowed(authResult.Failure.FailureReasons.First().Message);
                }
            }

            var fileName = _aasService.GetFileByPath(decodedAasIdentifier, decodedSubmodelIdentifier, idShortPath, out byte[] fileContent, out long fileSize);

            var fragmentObject = _fragmentRetrievalService.GetFragmentObject(fileContent, fragmentType, decodedFragment);

            var fragmentValue = _fragmentConverterService.ConvertFragmentObject(fragmentObject, content, level, extent);

            return new ObjectResult(fragmentValue);
        }
    }
}
