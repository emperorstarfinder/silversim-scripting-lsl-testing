// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

#pragma warning disable RCS1029, IDE0018, IDE0019

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scripting.Lsl.Api.ByteString;
using SilverSim.Scripting.Lsl.Event;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace SilverSim.Scripting.Lsl
{
    public partial class Script
    {
        private void LogInvokeException(string name, Exception e)
        {
            string state_name = m_CurrentState.GetType().FullName;
            state_name = state_name.Substring(1 + state_name.LastIndexOf('.'));
            if (e.InnerException != null)
            {
                e = e.InnerException;
            }

            string objectName;
            UUID objectID;
            try
            {
                objectName = Part.ObjectGroup.Name;
            }
            catch
            {
                objectName = "<Unknown>";
            }

            try
            {
                objectID = Part.ObjectGroup.ID;
            }
            catch
            {
                objectID = UUID.Zero;
            }
            if (e is InvalidProgramException || e is MethodAccessException || e is CallDepthLimitViolationException)
            {
                m_Log.ErrorFormat("Stopping script {5} (asset {6}) in {7} ({8}) [{9} ({10})]\nWithin state {0} event {1}:\nException {2} at script execution: {3}\n{4}",
                    state_name, name,
                    e.GetType().FullName, e.Message, e.StackTrace,
                    Item.Name, Item.AssetID.ToString(), Part.Name, Part.ID.ToString(), objectName, objectID.ToString());
                IsRunning = false;
            }
            else
            {
                m_Log.ErrorFormat("Script {5} (asset {6}) in {7} ({8}) [{9} ({10})]\nWithin state {0} event {1}:\nException {2} at script execution: {3}\n{4}",
                    state_name, name,
                    e.GetType().FullName, e.Message, e.StackTrace,
                    Item.Name, Item.AssetID.ToString(), Part.Name, Part.ID.ToString(), objectName, objectID.ToString());
            }
        }

        public static void InvokeStateEvent(Script script, string name, object[] param)
        {
            script.InvokeStateEventReal(name, param);
        }

        private void InvokeStateEvent(string name, params object[] param)
        {
            InvokeStateEventReal(name, param);
        }

        internal void InvokeStateEventReal(string name, object[] param)
        {
            MethodInfo mi;
            if (!m_CurrentStateMethods.TryGetValue(name, out mi))
            {
                mi = m_CurrentState.GetType().GetMethod(name);
                m_CurrentStateMethods.Add(name, mi);
            }

            if (mi != null)
            {
                IncrementScriptEventCounter();
                try
                {
                    foreach (object p in param)
                    {
                        if (p == null)
                        {
                            var sb = new StringBuilder("Call failure at ");
                            sb.Append(name + "(");
                            bool first = true;
                            foreach (object pa in param)
                            {
                                if (!first)
                                {
                                    sb.Append(",");
                                }
                                first = false;
                                if (pa == null)
                                {
                                    sb.Append("null");
                                }
                                else if (pa is LSLKey || pa is string)
                                {
                                    sb.Append("\"" + pa + "\"");
                                }
                                else if (pa is char)
                                {
                                    sb.Append("\'" + pa + "\'");
                                }
                                else
                                {
                                    sb.Append(pa.ToString());
                                }
                            }
                            sb.Append(")");
                            m_Log.Debug(sb);
                            return;
                        }
                    }

                    mi.Invoke(m_CurrentState, param);
                }
                catch (ChangeStateException)
                {
                    throw;
                }
                catch (ResetScriptException)
                {
                    throw;
                }
                catch (TargetInvocationException e)
                {
                    Type innerType = e.InnerException.GetType();
                    if (innerType == typeof(NotImplementedException))
                    {
                        ShoutUnimplementedException(e.InnerException as NotImplementedException);
                        return;
                    }
                    else if (innerType == typeof(DeprecatedFunctionCalledException))
                    {
                        ShoutDeprecatedException(e.InnerException as DeprecatedFunctionCalledException);
                        return;
                    }
                    else if (innerType == typeof(HitSandboxLimitException))
                    {
                        ShoutError(new LocalizedScriptMessage(this, "HitSandboxLimit", "Hit Sandbox Limit"));
                        return;
                    }
                    else if (innerType == typeof(ScriptAbortException) ||
                        innerType == typeof(ChangeStateException) ||
                        innerType == typeof(ResetScriptException) ||
                        innerType == typeof(LocalizedScriptErrorException) ||
                        innerType == typeof(DivideByZeroException) ||
                        innerType == typeof(ThreadAbortException))
                    {
                        throw e.InnerException;
                    }
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (NotImplementedException e)
                {
                    ShoutUnimplementedException(e);
                }
                catch (DeprecatedFunctionCalledException e)
                {
                    ShoutDeprecatedException(e);
                }
                catch (InvalidProgramException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (HitSandboxLimitException)
                {
                    ShoutError(new LocalizedScriptMessage(this, "HitSandboxLimit", "Hit Sandbox Limit"));
                }
                catch (CallDepthLimitViolationException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(new LocalizedScriptMessage(this, "FunctionCallDepthLimitViolation", "Function call depth limit violation"));
                }
                catch (TargetParameterCountException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (TargetException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (NullReferenceException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (ArgumentException e)
                {
#if DEBUG
                    LogInvokeException(name, e);
#endif
                    ShoutError(e.Message);
                }
                catch (ScriptWorkerInterruptionException)
                {
                    throw;
                }
                catch (ThreadAbortException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
            }
        }

        internal void InvokeRpcEventReal(string name, object[] param)
        {
            var paratypes = new List<Type>();
            object[] realparams = new object[param.Length];
            for (int i = 0; i < param.Length; ++i)
            {
                object p = param[i];
                Type pType = p.GetType();
                paratypes.Add(pType);
                realparams[i] = p;
                MethodInfo restoreMethod = pType.GetMethod("RestoreFromSerialization", new Type[] { typeof(ScriptInstance) });
                if (restoreMethod != null)
                {
                    ConstructorInfo cInfo = pType.GetConstructor(new Type[] { pType });
                    p = cInfo.Invoke(new object[] { p });
                    restoreMethod.Invoke(p, new object[] { this });
                }
                else if (pType == typeof(AnArray) || Attribute.GetCustomAttribute(pType, typeof(APICloneOnAssignmentAttribute)) != null)
                {
                    ConstructorInfo cInfo = pType.GetConstructor(new Type[] { pType });
                    p = cInfo.Invoke(new object[] { p });
                }
            }
            Type scriptType = GetType();

            MethodInfo mi = scriptType.GetMethod("rpcfn_" + name, paratypes.ToArray());

            if (mi != null)
            {
                if (mi.GetCustomAttribute(typeof(RpcLinksetExternalCallAllowedAttribute)) == null)
                {
                    if (!(Part?.ObjectGroup?.ContainsKey(RpcRemoteKey.AsUUID) ?? false))
                    {
                        /* ignore RPC from outside if not enabled */
                        return;
                    }
                }
                else if (mi.GetCustomAttribute(typeof(RpcLinksetExternalCallEveryoneAttribute)) == null)
                {
                    /* lockout foreign callers */
                    ObjectPart otherPart = null;
                    if (!(Part?.ObjectGroup?.Scene?.Primitives.TryGetValue(RpcRemoteKey.AsUUID, out otherPart) ?? false))
                    {
                        return;
                    }
                    if (mi.GetCustomAttribute(typeof(RpcLinksetExternalCallSameGroupAttribute)) != null &&
                        otherPart.Group.Equals(Part.Group))
                    {
                        /* same group is allowed */
                    }
                    else if (!otherPart.Owner.EqualsGrid(Part.Owner))
                    {
                        return;
                    }
                }
                IncrementScriptEventCounter();
                try
                {
                    foreach (object p in param)
                    {
                        if (p == null)
                        {
                            var sb = new StringBuilder("Call failure at rpc ");
                            sb.Append(name + "(");
                            bool first = true;
                            foreach (object pa in param)
                            {
                                if (!first)
                                {
                                    sb.Append(",");
                                }
                                first = false;
                                if (pa == null)
                                {
                                    sb.Append("null");
                                }
                                else if (pa is LSLKey || pa is string)
                                {
                                    sb.Append("\"" + pa + "\"");
                                }
                                else if (pa is char)
                                {
                                    sb.Append("\'" + pa + "\'");
                                }
                                else
                                {
                                    sb.Append(pa.ToString());
                                }
                            }
                            sb.Append(")");
                            m_Log.Debug(sb);
                            return;
                        }
                    }

                    mi.Invoke(this, realparams);
                }
                catch (ChangeStateException)
                {
                    throw;
                }
                catch (ResetScriptException)
                {
                    throw;
                }
                catch (TargetInvocationException e)
                {
                    Type innerType = e.InnerException.GetType();
                    if (innerType == typeof(NotImplementedException))
                    {
                        ShoutUnimplementedException(e.InnerException as NotImplementedException);
                        return;
                    }
                    else if (innerType == typeof(DeprecatedFunctionCalledException))
                    {
                        ShoutDeprecatedException(e.InnerException as DeprecatedFunctionCalledException);
                        return;
                    }
                    else if (innerType == typeof(HitSandboxLimitException))
                    {
                        ShoutError(new LocalizedScriptMessage(this, "HitSandboxLimit", "Hit Sandbox Limit"));
                    }
                    else if (innerType == typeof(ChangeStateException) ||
                        innerType == typeof(ResetScriptException) ||
                        innerType == typeof(LocalizedScriptErrorException) ||
                        innerType == typeof(DivideByZeroException) ||
                        innerType == typeof(ThreadAbortException))
                    {
                        throw e.InnerException;
                    }
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (NotImplementedException e)
                {
                    ShoutUnimplementedException(e);
                }
                catch (DeprecatedFunctionCalledException e)
                {
                    ShoutDeprecatedException(e);
                }
                catch (InvalidProgramException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (HitSandboxLimitException)
                {
                    ShoutError(new LocalizedScriptMessage(this, "HitSandboxLimit", "Hit Sandbox Limit"));
                }
                catch (CallDepthLimitViolationException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(new LocalizedScriptMessage(this, "FunctionCallDepthLimitViolation", "Function call depth limit violation"));
                }
                catch (TargetParameterCountException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (TargetException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (NullReferenceException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (ArgumentException e)
                {
#if DEBUG
                    LogInvokeException(name, e);
#endif
                    ShoutError(e.Message);
                }
                catch (ScriptWorkerInterruptionException)
                {
                    throw;
                }
                catch (ThreadAbortException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
            }
        }

        internal void InvokeNamedTimerEventReal(string name)
        {
            Type scriptType = GetType();

            MethodInfo mi = scriptType.GetMethod("timerfn_" + name, Type.EmptyTypes);

            if (mi != null)
            {
                IncrementScriptEventCounter();
                try
                {
                    mi.Invoke(this, new object[0]);
                }
                catch (ChangeStateException)
                {
                    throw;
                }
                catch (ResetScriptException)
                {
                    throw;
                }
                catch (TargetInvocationException e)
                {
                    Type innerType = e.InnerException.GetType();
                    if (innerType == typeof(NotImplementedException))
                    {
                        ShoutUnimplementedException(e.InnerException as NotImplementedException);
                        return;
                    }
                    else if (innerType == typeof(DeprecatedFunctionCalledException))
                    {
                        ShoutDeprecatedException(e.InnerException as DeprecatedFunctionCalledException);
                        return;
                    }
                    else if (innerType == typeof(HitSandboxLimitException))
                    {
                        ShoutError(new LocalizedScriptMessage(this, "HitSandboxLimit", "Hit Sandbox Limit"));
                    }
                    else if (innerType == typeof(ChangeStateException) ||
                        innerType == typeof(ResetScriptException) ||
                        innerType == typeof(LocalizedScriptErrorException) ||
                        innerType == typeof(DivideByZeroException) ||
                        innerType == typeof(ThreadAbortException))
                    {
                        throw e.InnerException;
                    }
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (NotImplementedException e)
                {
                    ShoutUnimplementedException(e);
                }
                catch (DeprecatedFunctionCalledException e)
                {
                    ShoutDeprecatedException(e);
                }
                catch (InvalidProgramException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (HitSandboxLimitException)
                {
                    ShoutError(new LocalizedScriptMessage(this, "HitSandboxLimit", "Hit Sandbox Limit"));
                }
                catch (CallDepthLimitViolationException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(new LocalizedScriptMessage(this, "FunctionCallDepthLimitViolation", "Function call depth limit violation"));
                }
                catch (TargetParameterCountException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (TargetException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (NullReferenceException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (ArgumentException e)
                {
#if DEBUG
                    LogInvokeException(name, e);
#endif
                    ShoutError(e.Message);
                }
                catch (ScriptWorkerInterruptionException)
                {
                    throw;
                }
                catch (ThreadAbortException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
            }
        }

        private static string MapTypeToString(Type t)
        {
            if (typeof(string) == t)
            {
                return "string";
            }
            else if (typeof(int) == t)
            {
                return "integer";
            }
            else if (typeof(char) == t)
            {
                return "char";
            }
            else if (typeof(long) == t)
            {
                return "long";
            }
            else if (typeof(Quaternion) == t)
            {
                return "rotation";
            }
            else if (typeof(AnArray) == t)
            {
                return "list";
            }
            else if (typeof(Vector3) == t)
            {
                return "vector";
            }
            else if (typeof(double) == t)
            {
                return "float";
            }
            else if (typeof(LSLKey) == t)
            {
                return "key";
            }
            else
            {
                return "???";
            }
        }

        private void ShoutDeprecatedException(DeprecatedFunctionCalledException e)
        {
            MethodBase mb = e.TargetSite;
            var apiLevel = (APILevelAttribute)Attribute.GetCustomAttribute(mb, typeof(APILevelAttribute));
            if (apiLevel != null)
            {
                string methodName = mb.Name;
                if (!string.IsNullOrEmpty(apiLevel.Name))
                {
                    methodName = apiLevel.Name;
                }

                var funcSignature = new StringBuilder(methodName + "(");

                ParameterInfo[] pi = mb.GetParameters();
                for (int i = 1; i < pi.Length; ++i)
                {
                    if (i > 1)
                    {
                        funcSignature.Append(", ");
                    }
                    funcSignature.Append(MapTypeToString(pi[i].ParameterType));
                }
                funcSignature.Append(")");

                ShoutError(new LocalizedScriptMessage(apiLevel, "ScriptCalledDeprecatedFunction0", "Script called deprecated function {0}", funcSignature.ToString()));
            }
        }

        private void ShoutUnimplementedException(NotImplementedException e)
        {
            MethodBase mb = e.TargetSite;
            var apiLevel = (APILevelAttribute)Attribute.GetCustomAttribute(mb, typeof(APILevelAttribute));
            if (apiLevel != null)
            {
                string methodName = mb.Name;
                if (!string.IsNullOrEmpty(apiLevel.Name))
                {
                    methodName = apiLevel.Name;
                }

                var funcSignature = new StringBuilder(methodName + "(");

                ParameterInfo[] pi = mb.GetParameters();
                for (int i = 1; i < pi.Length; ++i)
                {
                    if (i > 1)
                    {
                        funcSignature.Append(", ");
                    }
                    funcSignature.Append(MapTypeToString(pi[i].ParameterType));
                }
                funcSignature.Append(")");

                ShoutError(new LocalizedScriptMessage(apiLevel, "ScriptCalledUnimplementedFunction0", "Script called unimplemented function {0}", funcSignature.ToString()));
            }
        }

        public override void ProcessEvent()
        {
            long startticks = TimeSource.TickCount;
            bool executeStateEntry = false;
            bool executeStateExit = false;
            bool executeScriptReset = false;
            ILSLState newState = m_CurrentState;

            do
            {
                if (newState == null)
                {
                    newState = m_States["default"];
                    executeStateEntry = true;
                    executeStateExit = false;
                }
                else if (newState != m_CurrentState)
                {
                    executeStateExit = true;
                    executeStateEntry = true;
                }

                #region Script Reset
                if (executeScriptReset)
                {
                    executeScriptReset = false;
                    executeStateExit = false;
                    executeStateEntry = true;
                    SetTimerEvent(0);
                    foreach(TimerInfo ti in m_Timers.Values)
                    {
                        ti.SetTimerEvent(0);
                    }
                    TriggerOnStateChange();
                    TriggerOnScriptReset();
                    m_Events.Clear();
                    m_HaveQueuedTimerEvent = false;
                    foreach(TimerInfo ti in m_Timers.Values)
                    {
                        ti.AckTimer();
                    }
                    lock (m_Lock)
                    {
                        m_ExecutionTime = 0f;
                    }
                    newState = m_States["default"];
                    SetCurrentState(newState);
                    StartParameter = 0;
                    ResetVariables();
                    lock (this) /* prevent aborting inside UpdateScriptState() */
                    {
                        UpdateScriptState();
                    }
                    startticks = TimeSource.TickCount;
                }
                #endregion

                #region State Exit
                bool executedStateExit = executeStateExit;
                try
                {
                    if (executeStateExit)
                    {
                        if (!InheritEventsOnStateChange)
                        {
                            lock (this) /* prevent aborting inside UpdateScriptState() */
                            {
                                m_Events.RemoveIf((e) => e.GetType() != typeof(ResetScriptEvent));
                            }
                        }
                        executeStateExit = false;
                        startticks = TimeSource.TickCount;
                        InvokeStateEvent("state_exit");
                    }
                }
                catch (ResetScriptException)
                {
                    executeScriptReset = true;
                    continue;
                }
                catch (ChangeStateException e)
                {
                    ShoutError(new LocalizedScriptMessage(e, "ScriptErrorStateChangeUsedInStateExit", "Script error! state change used in state_exit"));
                    LogInvokeException("state_exit", e);
                }
                catch (LocalizedScriptErrorException e)
                {
                    ShoutError(new LocalizedScriptMessage(e.NlsRefObject, e.NlsId, e.NlsDefMsg, e.NlsParams));
                }
                catch (DivideByZeroException)
                {
                    ShoutError(new LocalizedScriptMessage(this, "DivisionByZeroEncountered", "Division by zero encountered"));
                }
                finally
                {
                    if (executedStateExit)
                    {
                        lock (m_Lock)
                        {
                            m_ExecutionTime += TimeSource.TicksToSecs(TimeSource.TicksElapsed(TimeSource.TickCount, startticks));
                        }
                    }
                }
                if (executedStateExit)
                {
                    lock (this) /* really needed to prevent aborting here */
                    {
                        UpdateScriptState();
                    }
                }
                #endregion

                #region State Entry
                bool executedStateEntry = executeStateEntry;
                try
                {
                    if (executeStateEntry)
                    {
                        executeStateEntry = false;
                        SetCurrentState(newState);
                        startticks = TimeSource.TickCount;
                        lock (this)
                        {
                            /* lock(this) needed here to prevent aborting in wrong place */
                            Part.UpdateScriptFlags();
                        }

                        InvokeStateEvent("state_entry");
                    }
                }
                catch (ResetScriptException e)
                {
                    if (m_States["default"] == m_CurrentState)
                    {
                        ShoutError(new LocalizedScriptMessage(e.Message, "llResetScriptUsedInDefaultStateEntry", "Script error! llResetScript used in default.state_entry"));
                        return;
                    }
                    else
                    {
                        executeScriptReset = true;
                        continue;
                    }
                }
                catch (ChangeStateException e)
                {
                    if (m_CurrentState != m_States[e.NewState])
                    {
                        /* if state is equal, it simply aborts the event execution */
                        newState = m_States[e.NewState];
                        executeStateExit = true;
                        executeStateEntry = true;

                        lock (this) /* really needed to prevent aborting here */
                        {
                            UpdateScriptState();
                            if (!InheritEventsOnStateChange)
                            {
                                m_Events.RemoveIf((ev) => ev.GetType() != typeof(ResetScriptEvent));
                            }
                        }
                    }
                }
                catch (LocalizedScriptErrorException e)
                {
                    ShoutError(new LocalizedScriptMessage(e.NlsRefObject, e.NlsId, e.NlsDefMsg, e.NlsParams));
                }
                catch (DivideByZeroException)
                {
                    ShoutError(new LocalizedScriptMessage(this, "DivisionByZeroEncountered", "Division by zero encountered"));
                }
                finally
                {
                    if (executedStateEntry)
                    {
                        lock (m_Lock)
                        {
                            m_ExecutionTime += TimeSource.TicksToSecs(TimeSource.TicksElapsed(TimeSource.TickCount, startticks));
                        }
                    }
                }

                if (executedStateEntry)
                {
                    lock (this) /* really needed to prevent aborting here */
                    {
                        UpdateScriptState();
                    }
                }
                #endregion

                #region Event Logic
                int eventsCount = m_Events.Count;
                if (!InheritEventsOnStateChange && executeStateEntry && eventsCount > 1)
                {
                    eventsCount = 1;
                }
                processAgain:
                {
                    bool eventExecuted = false;
                    try
                    {
                        IScriptEvent ev;
                        try
                        {
                            ev = m_Events.Dequeue();
                        }
                        catch
                        {
                            ev = null;
                        }

                        Type evType = ev?.GetType();
                        if (evType == typeof(ResetScriptEvent))
                        {
                            executeScriptReset = true;
                            eventsCount = 0;
                            ev = null;
                        }

                        if (ev != null)
                        {
                            eventExecuted = true;
                            startticks = Environment.TickCount;
                            Type evt = ev.GetType();
                            Action<Script, IScriptEvent> evtDelegate;
                            if (StateEventHandlers.TryGetValue(evt, out evtDelegate))
                            {
                                var detectedEv = ev as IScriptDetectedEvent;
                                if (detectedEv != null)
                                {
                                    m_Detected = detectedEv.Detected;
                                }
                                evtDelegate(this, ev);
                            }
                        }
                    }
                    catch (ResetScriptException)
                    {
                        executeScriptReset = true;
                        eventsCount = 0;
                        continue;
                    }
                    catch (ChangeStateException e)
                    {
                        ILSLState newTargetedState;
                        if (m_States.TryGetValue(e.NewState, out newTargetedState))
                        {
                            if (m_CurrentState != newTargetedState)
                            {
                                /* if state is equal, it simply aborts the event execution */
                                newState = newTargetedState;
                                eventsCount = 0;
                                executeStateExit = true;
                                executeStateEntry = true;
                            }
                        }
                        else
                        {
                            m_Log.ErrorFormat("Invalid state {0} at script {1} in {2} [{3}]", e.NewState, Item.Name, Part.Name, Part.ID);
                        }
                    }
                    catch (LocalizedScriptErrorException e)
                    {
                        ShoutError(new LocalizedScriptMessage(e.NlsRefObject, e.NlsId, e.NlsDefMsg, e.NlsParams));
                    }
                    catch (DivideByZeroException)
                    {
                        ShoutError(new LocalizedScriptMessage(this, "DivisionByZeroEncountered", "Division by zero encountered"));
                    }
                    finally
                    {
                        if (eventExecuted)
                        {
                            lock (m_Lock)
                            {
                                m_ExecutionTime += TimeSource.TicksToSecs(TimeSource.TicksElapsed(TimeSource.TickCount, startticks));
                            }
                        }
                    }
                    #endregion

                    lock (this) /* really needed to prevent aborting here */
                    {
                        UpdateScriptState();
                    }
                }
                if(eventsCount-->1)
                {
                    goto processAgain;
                }
            } while (executeStateEntry || executeStateExit || executeScriptReset);
        }

        public override bool HasEventsPending => m_Events.Count != 0;

        #region Event to function handlers

        static Script()
        {
            #region Default state event handlers
            StateEventHandlers.Add(typeof(AtRotTargetEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (AtRotTargetEvent)ev;
                script.InvokeStateEvent("at_rot_target", e.Handle, e.TargetRotation, e.OurRotation);
            });

            StateEventHandlers.Add(typeof(AttachEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (AttachEvent)ev;
                script.InvokeStateEvent("attach", new LSLKey(e.ObjectID));
            });

            StateEventHandlers.Add(typeof(AtTargetEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (AtTargetEvent)ev;
                script.InvokeStateEvent("at_target", e.Handle, e.TargetPosition, e.OurPosition);
            });

            StateEventHandlers.Add(typeof(ChangedEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (ChangedEvent)ev;
                script.InvokeStateEvent("changed", (int)e.Flags);
            });

            StateEventHandlers.Add(typeof(CollisionEvent), HandleCollision);

            StateEventHandlers.Add(typeof(DataserverEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (DataserverEvent)ev;
                script.InvokeStateEvent("dataserver", new LSLKey(e.QueryID), e.Data);
            });

            StateEventHandlers.Add(typeof(MessageObjectEvent), HandleMessageObject);

            StateEventHandlers.Add(typeof(EmailEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (EmailEvent)ev;
                script.InvokeStateEvent("email", e.Time, e.Address, e.Subject, e.Message, e.NumberLeft);
            });

            StateEventHandlers.Add(typeof(HttpResponseEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (HttpResponseEvent)ev;
                if (e.UsesByteArray)
                {
                    script.InvokeStateEvent("http_binary_response", new LSLKey(e.RequestID), e.Status, e.Metadata, new ByteArrayApi.ByteArray(e.Body));
                }
                else
                {
                    script.InvokeStateEvent("http_response", new LSLKey(e.RequestID), e.Status, e.Metadata, e.Body.FromUTF8Bytes());
                }
            });

            StateEventHandlers.Add(typeof(HttpRequestEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (HttpRequestEvent)ev;
                if (e.UsesByteArray)
                {
                    script.InvokeStateEvent("http_binary_request", new LSLKey(e.RequestID), e.Method, new ByteArrayApi.ByteArray(e.Body));
                }
                else
                {
                    script.InvokeStateEvent("http_request", new LSLKey(e.RequestID), e.Method, e.Body.FromUTF8Bytes());
                }
            });

            StateEventHandlers.Add(typeof(LandCollisionEvent), HandleLandCollision);

            StateEventHandlers.Add(typeof(LinkMessageEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (LinkMessageEvent)ev;
                script.InvokeStateEvent("link_message", e.SenderNumber, e.Number, e.Data, new LSLKey(e.Id));
            });

            StateEventHandlers.Add(typeof(ListenEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (ListenEvent)ev;
                script.InvokeStateEvent("listen", e.Channel, e.Name, new LSLKey(e.ID), e.Message);
            });

            StateEventHandlers.Add(typeof(MoneyEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (MoneyEvent)ev;
                script.InvokeStateEvent("money", new LSLKey(e.ID), e.Amount);
            });

            StateEventHandlers.Add(typeof(MovingStartEvent), (Script script, IScriptEvent ev) => script.InvokeStateEvent("moving_start"));

            StateEventHandlers.Add(typeof(MovingEndEvent), (Script script, IScriptEvent ev) => script.InvokeStateEvent("moving_end"));

            StateEventHandlers.Add(typeof(NoSensorEvent), (Script script, IScriptEvent ev) => script.InvokeStateEvent("no_sensor"));

            StateEventHandlers.Add(typeof(NotAtRotTargetEvent), (Script script, IScriptEvent ev) => script.InvokeStateEvent("not_at_rot_target"));

            StateEventHandlers.Add(typeof(NotAtTargetEvent), (Script script, IScriptEvent ev) => script.InvokeStateEvent("not_at_target"));

            StateEventHandlers.Add(typeof(ObjectRezEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (ObjectRezEvent)ev;
                script.InvokeStateEvent("object_rez", new LSLKey(e.ObjectID));
            });

            StateEventHandlers.Add(typeof(OnRezEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (OnRezEvent)ev;
                script.StartParameter = new Integer(e.StartParam);
                script.InvokeStateEvent("on_rez", e.StartParam);
            });

            StateEventHandlers.Add(typeof(PathUpdateEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (PathUpdateEvent)ev;
                script.InvokeStateEvent("path_update", e.Type, e.Reserved);
            });

            StateEventHandlers.Add(typeof(RemoteDataEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (RemoteDataEvent)ev;
                script.InvokeStateEvent("remote_data", e.Type, new LSLKey(e.Channel), new LSLKey(e.MessageID), e.Sender, e.IData, e.SData);
            });

            StateEventHandlers.Add(typeof(ResetScriptEvent), (Script script, IScriptEvent ev) =>
#pragma warning disable RCS1021 // Simplify lambda expression.
            {
                throw new ResetScriptException();
            });
#pragma warning restore RCS1021 // Simplify lambda expression.

            StateEventHandlers.Add(typeof(ItemSoldEvent), (script, ev) =>
            {
                var e = (ItemSoldEvent)ev;
                UGUIWithName agent = script.Part?.ObjectGroup?.Scene?.AvatarNameService?.ResolveName(e.Agent) ?? (UGUIWithName)e.Agent;
                script.InvokeStateEvent("item_sold", agent.FullName, new LSLKey(e.Agent.ID), e.ObjectName, new LSLKey(e.ObjectID));
            });

            StateEventHandlers.Add(typeof(SensorEvent), (script, ev) => script.InvokeStateEvent("sensor", script.m_Detected.Count));
            StateEventHandlers.Add(typeof(RuntimePermissionsEvent), HandleRuntimePermissions);
            StateEventHandlers.Add(typeof(ExperiencePermissionsEvent), HandleExperiencePermissions);
            StateEventHandlers.Add(typeof(ExperiencePermissionsDeniedEvent), HandleExperiencePermissionsDenied);
            StateEventHandlers.Add(typeof(TouchEvent), HandleTouch);
            StateEventHandlers.Add(typeof(TimerEvent), HandleTimerEvent);
            StateEventHandlers.Add(typeof(NamedTimerEvent), HandleNamedTimerEvent);
            StateEventHandlers.Add(typeof(RpcScriptEvent), HandleRpcScriptEvent);
            StateEventHandlers.Add(typeof(ControlEvent), (script, ev) =>
            {
                var e = (ControlEvent)ev;
                script.InvokeStateEvent("control", new LSLKey(e.AgentID), (int)e.Level, (int)e.Edge);
            });
            StateEventHandlers.Add(typeof(TransactionResultEvent), (script, ev) =>
            {
                var e = (TransactionResultEvent)ev;
                script.InvokeStateEvent("transaction_result", new LSLKey(e.TransactionID), e.Success.ToLSLBoolean(), e.ReplyData);
            });
            #endregion
        }

        private static void HandleNamedTimerEvent(Script script, IScriptEvent ev)
        {
            var nev = (NamedTimerEvent)ev;
            TimerInfo ti;
            if(script.m_Timers.TryGetValue(nev.TimerName, out ti))
            {
                ti.AckTimer();
                script.ActiveNamedTimer = nev.TimerName;
                script.IsInTimerEvent = true;
                try
                {
                    script.InvokeNamedTimerEventReal(nev.TimerName);
                }
                finally
                {
                    script.IsInTimerEvent = false;
                    script.ActiveNamedTimer = string.Empty;
                }
            }
        }

        private static void HandleTimerEvent(Script script, IScriptEvent ev)
        {
            script.m_HaveQueuedTimerEvent = false;
            script.ActiveNamedTimer = string.Empty;
            script.IsInTimerEvent = true;
            try
            {
                script.InvokeStateEvent("timer");
            }
            finally
            {
                script.IsInTimerEvent = false;
            }
        }

        private static void HandleCollision(Script script, IScriptEvent ev)
        {
            switch (((CollisionEvent)ev).Type)
            {
                case CollisionEvent.CollisionType.Start:
                    script.InvokeStateEvent("collision_start", script.m_Detected.Count);
                    break;

                case CollisionEvent.CollisionType.End:
                    script.InvokeStateEvent("collision_end", script.m_Detected.Count);
                    break;

                case CollisionEvent.CollisionType.Continuous:
                    script.InvokeStateEvent("collision", script.m_Detected.Count);
                    break;
            }
        }

        private static void HandleMessageObject(Script script, IScriptEvent ev)
        {
            var e = (MessageObjectEvent)ev;
            if (script.UseMessageObjectEvent)
            {
                script.InvokeStateEvent("object_message", new LSLKey(e.ObjectID), e.Data);
            }
            else
            {
                script.InvokeStateEvent("dataserver", new LSLKey(e.ObjectID), e.Data);
            }
        }

        private static void HandleLandCollision(Script script, IScriptEvent ev)
        {
            var e = (LandCollisionEvent)ev;
            switch (e.Type)
            {
                case LandCollisionEvent.CollisionType.Start:
                    script.InvokeStateEvent("land_collision_start", e.Position);
                    break;

                case LandCollisionEvent.CollisionType.End:
                    script.InvokeStateEvent("land_collision_end", e.Position);
                    break;

                case LandCollisionEvent.CollisionType.Continuous:
                    script.InvokeStateEvent("land_collision", e.Position);
                    break;
            }
        }

        private static void HandleRuntimePermissions(Script script, IScriptEvent ev)
        {
            var e = (RuntimePermissionsEvent)ev;
            if (e.PermissionsKey != script.Item.Owner)
            {
                e.Permissions &= ~(ScriptPermissions.Debit | ScriptPermissions.SilentEstateManagement | ScriptPermissions.ChangeLinks);
            }
            if (e.PermissionsKey != script.Item.Owner)
            {
#warning Add group support here (also allowed are group owners)
                e.Permissions &= ~ScriptPermissions.ReturnObjects;
            }
            if (script.Item.IsGroupOwned)
            {
                e.Permissions &= ~ScriptPermissions.Debit;
            }

            lock (script) /* ensure that no script abort is happening here */
            {
                ObjectPartInventoryItem.PermsGranterInfo oldInfo = script.Item.PermsGranter;
                if (oldInfo.DebitPermissionKey != UUID.Zero)
                {
                    /* TODO: hand off to economy handling for revocation */
                }
                UUID debitPermissionKey = UUID.Zero;
                if ((e.Permissions & ScriptPermissions.Debit) != 0)
                {
                    /* hand off to economy handling */
                    IAgent agent;
                    SceneInterface scene = script.Part.ObjectGroup.Scene;
                    if (scene.Agents.TryGetValue(e.PermissionsKey.ID, out agent))
                    {
                        try
                        {
                            debitPermissionKey = agent.EconomyService.RequestScriptDebitPermission(
                                new DebitPermissionRequestData
                                {
                                    SourceID = e.PermissionsKey,
                                    RegionID = scene.ID,
                                    ObjectID = script.Part.ID,
                                    ObjectName = script.Part.RootPart.Name,
                                    ObjectDescription = script.Part.RootPart.Description,
                                    ItemID = script.Item.ID
                                });
                        }
                        catch
                        {
                            e.Permissions &= ~ScriptPermissions.Debit;
                        }
                    }
                    else
                    {
                        /* agent not there */
                        e.Permissions &= ~ScriptPermissions.Debit;
                    }
                }
            }

            script.Item.PermsGranter = new ObjectPartInventoryItem.PermsGranterInfo
            {
                PermsGranter = e.PermissionsKey,
                PermsMask = e.Permissions
            };
            script.InvokeStateEvent("run_time_permissions", (int)e.Permissions);
        }

        private static void HandleExperiencePermissions(Script script, IScriptEvent ev)
        {
            var e = (ExperiencePermissionsEvent)ev;

            script.Item.PermsGranter = new ObjectPartInventoryItem.PermsGranterInfo
            {
                PermsGranter = e.PermissionsKey,
                PermsMask = ScriptPermissions.TakeControls | ScriptPermissions.TriggerAnimation | ScriptPermissions.Attach | ScriptPermissions.TrackCamera | ScriptPermissions.ControlCamera | ScriptPermissions.Teleport
            };
            script.InvokeStateEvent("experience_permissions", new LSLKey(e.PermissionsKey.ID));
        }

        private static void HandleExperiencePermissionsDenied(Script script, IScriptEvent ev)
        {
            var e = (ExperiencePermissionsDeniedEvent)ev;
            script.InvokeStateEvent("experience_permissions_denied", new LSLKey(e.AgentId.ID), e.Reason);
        }

        public string RpcRemoteScriptName { get; private set; }
        public int RpcRemoteLinkNumber { get; private set; }
        public LSLKey RpcRemoteKey { get; private set; }
        public LSLKey RpcRemoteScriptKey { get; private set; }

        private static void HandleRpcScriptEvent(Script script, IScriptEvent ev)
        {
            var e = (RpcScriptEvent)ev;
            script.RpcRemoteKey = e.SenderKey;
            script.RpcRemoteLinkNumber = e.SenderLinkNumber;
            script.RpcRemoteScriptName = e.SenderScriptName;
            script.RpcRemoteScriptKey = e.SenderScriptKey;
            script.InvokeRpcEventReal(e.FunctionName, e.Parameters);
            script.RpcRemoteKey = UUID.Zero;
            script.RpcRemoteLinkNumber = -1;
            script.RpcRemoteScriptName = string.Empty;
            script.RpcRemoteScriptKey = UUID.Zero;
        }

        private static void HandleTouch(Script script, IScriptEvent ev)
        {
            var e = (TouchEvent)ev;
            switch (e.Type)
            {
                case TouchEvent.TouchType.Start:
                    script.InvokeStateEvent("touch_start", script.m_Detected.Count);
                    break;

                case TouchEvent.TouchType.End:
                    script.InvokeStateEvent("touch_end", script.m_Detected.Count);
                    break;

                case TouchEvent.TouchType.Continuous:
                    script.InvokeStateEvent("touch", script.m_Detected.Count);
                    break;
            }
        }
        #endregion
    }
}
