using CrashNest.Attributes;
using CrashNest.Common.Domain;
using CrashNest.Common.RequestModels;
using CrashNest.Common.Storage;
using Microsoft.AspNetCore.Mvc;
using SqlKata;

namespace CrashNest.Controllers {

    [ApiController, JsonIn, JsonOut, Route ( "/api/reports" )]
    public class ErrorReports {

        private readonly IStorageContext m_storageContext;

        public ErrorReports ( IStorageContext storageContext ) => m_storageContext = storageContext ?? throw new ArgumentNullException ( nameof ( storageContext ) );

        [HttpPost ( "save" )]
        public async Task Save ( [FromBody, RequiredParameter] RegisterReportModel model ) {
            if ( model == null ) throw new ArgumentNullException ( nameof ( model ) );

            model.Report.Id = Guid.Empty;

            await m_storageContext.AddOrUpdate ( model.Report );

            foreach ( var metadata in model.Metadata ) {
                metadata.Id = Guid.Empty;
                metadata.ErrorReportId = model.Report.Id;
            }
            await m_storageContext.MultiAddOrUpdate ( model.Metadata );
        }

        [HttpPost ( "byfilter" )]
        public async Task<IEnumerable<ErrorReport>> ByFilter ( [FromBody, RequiredParameter] ReportFilterListModel model ) {
            if ( model == null ) throw new ArgumentNullException ( nameof ( model ) );

            return await m_storageContext.GetAsync<ErrorReport> (
                new Query ( nameof ( ErrorReport ) )
            );
        }

    }

}
