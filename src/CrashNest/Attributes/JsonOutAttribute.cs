using Microsoft.AspNetCore.Mvc;

namespace CrashNest.Attributes {

    /// <summary>
    /// Which controller/endpoint output format. Shortcut for Json.
    /// </summary>
    public class JsonOutAttribute : ProducesAttribute {

        public JsonOutAttribute () : base ( "application/json" ) {
        }

    }

}
