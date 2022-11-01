using CrashNest.Common.Attributes;
using CrashNest.Storage.Migrator;

namespace CrashNest.Storage.Migrations {

    [Migration ( 1667292820, "Creating NotificationRule table" )]
    public class MigrationCreateNotificationRule : Migration {

        protected override void Up () {
            ExecuteCommand (
                @"
                    CREATE TABLE NotificationRule (
                        Id uuid NOT NULL DEFAULT uuid_generate_v4 (),
                        Name text NOT NULL,
                        Conditions jsonb,
                        SendSettings jsonb,
                        CONSTRAINT NotificationRule_pkey PRIMARY KEY (id)
                    )
                "
            );
        }

        protected override void Down () => ExecuteCommand ( "DROP TABLE NotificationRule" );

    }

}
