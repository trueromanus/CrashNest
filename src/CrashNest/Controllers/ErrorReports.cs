using CrashNest.Attributes;
using CrashNest.Common.Domain;
using CrashNest.Common.ResponseModels;
using CrashNest.Common.Storage;
using Microsoft.AspNetCore.Mvc;

namespace CrashNest.Controllers {

    [ApiController, JsonIn, JsonOut, Route ( "/api/reports" )]
    public class ErrorReports {

        private readonly IStorageContext m_storageContext;

        public ErrorReports ( IStorageContext storageContext ) {
            m_storageContext = storageContext ?? throw new ArgumentNullException ( nameof ( storageContext ) );
        }

        [HttpPost ( "save" )]
        public async Task<ReportResultModel> Save ( [FromBody, RequiredParameter] ErrorReport errorReport ) {
            if ( errorReport == null ) throw new ArgumentNullException ( nameof ( errorReport ) );

            try {
                await m_storageContext.AddOrUpdate ( errorReport );
                return new ReportResultModel ( "", true );
            } catch ( Exception exception ) {
                if ( exception is ArgumentException ) return new ReportResultModel ( exception.Message, false );

                return new ReportResultModel ( "Error while processing request.", false );
            }

        }

    }

}
