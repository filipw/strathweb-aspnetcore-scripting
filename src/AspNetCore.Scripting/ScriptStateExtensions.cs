using System.Reflection;
using Microsoft.CodeAnalysis.Scripting;

namespace AspNetCore.Scripting
{
    public static class ScriptStateExtensions
    {
        public static Assembly GetScriptAssembly(this ScriptState<object> scriptState)
        {
            var executionStateProperty = scriptState.GetType().GetProperty("ExecutionState", BindingFlags.NonPublic | BindingFlags.Instance);
            var executionState = executionStateProperty.GetValue(scriptState);

            var submissionStatesField = executionState.GetType().GetField("_submissionStates", BindingFlags.NonPublic | BindingFlags.Instance);
            var submissions = submissionStatesField.GetValue(executionState) as object[];

            // boohooo
            var scriptAssembly = submissions[1].GetType().GetTypeInfo().Assembly;
            return scriptAssembly;
        }
    }
}