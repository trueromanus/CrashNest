using CrashNest.Attributes;
using CrashNest.Common.RequestModels;
using CrashNest.Common.ResponseModels;
using CrashNest.Common.Storage;
using Microsoft.AspNetCore.Mvc;

namespace CrashNest.Controllers {

    [ApiController, JsonIn, JsonOut, Route ( "/api/reports" )]
    public class ErrorReports {

        private readonly IStorageContext m_storageContext;

        public ErrorReports ( IStorageContext storageContext ) => m_storageContext = storageContext ?? throw new ArgumentNullException ( nameof ( storageContext ) );

        [HttpPost ( "report" )]
        public async Task<ReportResultModel> Save ( [FromBody, RequiredParameter] RegisterReportModel model ) {
            if ( model == null ) throw new ArgumentNullException ( nameof ( model ) );

            model.Report.Id = Guid.Empty;

            await m_storageContext.AddOrUpdate ( model.Report );

            foreach ( var metadata in model.Metadata ) {
                metadata.ErrorReportId = model.Report.Id;
            }
            await m_storageContext.MultiAddOrUpdate ( model.Metadata );

            return new ReportResultModel ( "", true );
        }

    }

}
