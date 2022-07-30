using CrashNest.Attributes;
using CrashNest.Common.Domain;
using CrashNest.Common.ResponseModels;
using Microsoft.AspNetCore.Mvc;

namespace CrashNest.Controllers {

    [ApiController, JsonIn, JsonOut, Route ( "/api/reports" )]
    public class ErrorReports {

        [HttpPost ( "save" )]
        public ReportResultModel Save ( [FromBody, RequiredParameter] ErrorReport errorReport ) {
            if ( errorReport == null ) throw new ArgumentNullException (nameof( errorReport ) );

            //TODO: saving error reports to storage

            return new ReportResultModel ( "", true );
        }

    }

}
