﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18033
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Sensorium.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Sensorium.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Built action: {then}
        ///{action}.
        /// </summary>
        internal static string Brain_BuiltBehaviorAction {
            get {
                return ResourceManager.GetString("Brain_BuiltBehaviorAction", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Built query: {when}
        ///{query}.
        /// </summary>
        internal static string Brain_BuiltBehaviorQuery {
            get {
                return ResourceManager.GetString("Brain_BuiltBehaviorQuery", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Built state query: {when}
        ///{query}.
        /// </summary>
        internal static string Brain_BuiltStateQuery {
            get {
                return ResourceManager.GetString("Brain_BuiltStateQuery", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Configuring behavior: {behavior}.
        /// </summary>
        internal static string Brain_ConfiguringBehavior {
            get {
                return ResourceManager.GetString("Brain_ConfiguringBehavior", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Executing behavior action: {behavior}.
        /// </summary>
        internal static string Brain_ExecutingBehaviorAction {
            get {
                return ResourceManager.GetString("Brain_ExecutingBehaviorAction", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Event stream matched behavior condition: {behavior}.
        /// </summary>
        internal static string Brain_MatchedBehaviorCondition {
            get {
                return ResourceManager.GetString("Brain_MatchedBehaviorCondition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Topic &apos;{topic}&apos; in specified behavior &apos;{behavior}&apos; does not have a registered known type..
        /// </summary>
        internal static string Brain_UnknonwnBehaviorTopic {
            get {
                return ResourceManager.GetString("Brain_UnknonwnBehaviorTopic", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot convert command with payload of type &apos;{payload}&apos; to a device action. Supported types are: boolean, number, string and void..
        /// </summary>
        internal static string CommandExtensions_CannotConvertToDo {
            get {
                return ResourceManager.GetString("CommandExtensions_CannotConvertToDo", resourceCulture);
            }
        }
    }
}
