using CrashNest.Attributes;
using CrashNest.Common.Domain;
using CrashNest.Common.Storage;
using Microsoft.AspNetCore.Mvc;
using SqlKata;

namespace CrashNest.Controllers {

    [ApiController, JsonIn, JsonOut, Route ( "/api/notificationrules" )]
    public class NotificationRules {

        private readonly IStorageContext m_storageContext;

        public NotificationRules ( IStorageContext storageContext ) => m_storageContext = storageContext ?? throw new ArgumentNullException ( nameof ( storageContext ) );

        [HttpPost ( "create" )]
        public async Task Create ( [FromBody] NotificationRule notificationRule ) {
            notificationRule.Id = Guid.Empty;

            await m_storageContext.AddOrUpdate ( notificationRule );
        }

        [HttpPut ( "edit" )]
        public async Task Edit ( [FromBody] NotificationRule notificationRule ) => await m_storageContext.AddOrUpdate ( notificationRule );

        [HttpGet ( "all" )]
        public async Task<IEnumerable<NotificationRule>> GetAll () {
            var query = new Query ( nameof ( NotificationRule ) );
            return await m_storageContext.GetAsync<NotificationRule> ( query );
        }

    }

}
