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
            model.Report.Created = DateTime.UtcNow;

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

            var query = new Query ( nameof ( ErrorReport ) );

            FillDateRange ( model, query );
            if ( model.Codes.Any () ) query.WhereIn ( "code", model.Codes );
            if ( model.ErrorTypes.Any () ) query.WhereIn ( "errortype", model.ErrorTypes );
            if ( !string.IsNullOrEmpty ( model.Message ) ) query.WhereLike ( "message", model.Message );

            return await m_storageContext.GetAsync<ErrorReport> ( query );
        }

        private static void FillDateRange ( ReportFilterListModel model, Query query ) {
            if ( model.StartDate.HasValue && model.EndDate.HasValue ) {
                query
                    .Where ( "created", ">=", model.StartDate.Value )
                    .Where ( "created", "<=", model.EndDate.Value );
            } else {
                switch ( model.ListMode ) {
                    case ReportFilterListMode.LastFiveMinutes:
                        query.Where ( "created", ">=", DateTime.UtcNow.AddMinutes ( -5 ) );
                        break;
                    case ReportFilterListMode.LastHour:
                        query.Where ( "created", ">=", DateTime.UtcNow.AddHours ( -1 ) );
                        break;
                    case ReportFilterListMode.LastDay:
                        query.Where ( "created", ">=", DateTime.UtcNow.AddDays ( -1 ) );
                        break;
                    case ReportFilterListMode.LastWeek:
                        query.Where ( "created", ">=", DateTime.UtcNow.AddDays ( -7 ) );
                        break;
                    case ReportFilterListMode.LastMonth:
                        query.Where ( "created", ">=", DateTime.UtcNow.AddDays ( -31 ) );
                        break;
                    default: throw new NotSupportedException ( $"List mode {model.ListMode} not supported!" );
                }
            }
        }
    }

}
