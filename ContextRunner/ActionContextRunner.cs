﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ContextRunner.Base;
using ContextRunner.State;
using Newtonsoft.Json;

namespace ContextRunner
{
    public class ActionContextRunner : IContextRunner
    {
        public static ActionContextRunner Runner { get; protected set; }

        static ActionContextRunner()
        {
            Runner = new ActionContextRunner();
        }

        public static void Configure(
            Action<IActionContext> onStart = null,
            ActionContextSettings settings = null,
            IEnumerable<ISanitizer> sanitizers = null)
        {
            Runner = new ActionContextRunner(onStart, settings, sanitizers);
        }

        protected Action<IActionContext> OnStart { get; set; }
        protected Action<IActionContext> OnEnd{ get; set; }
        protected ActionContextSettings Settings { get; set; }
        protected IEnumerable<ISanitizer> Sanitizers { get; set; }
        
        private IDisposable _logHandle;

        public ActionContextRunner(
            Action<IActionContext> onStart = null,
            ActionContextSettings settings = null,
            IEnumerable<ISanitizer> sanitizers = null)
        {
            OnStart = onStart ?? Setup;
            Settings = settings ?? new ActionContextSettings();
            Sanitizers = sanitizers ?? new ISanitizer[0];
        }
        
        #region Base implementation stuff...

        private void Setup(IActionContext context)
        {
            _logHandle = context.Logger.WhenEntryLogged.Subscribe(
                _ => { },
                _ => LogContext(context),
                () => LogContext(context));
        }

        private void LogContext(IActionContext context)
        {
            var shouldLog = context.GetCheckpoints().Any() || context.Logger.LogEntries.Any();
            
            if (!shouldLog || context.ShouldSuppress())
            {
                return;
            }

            var summaries = ContextSummary.Summarize(context);
            var logLines = summaries.Select(s =>
                JsonConvert.SerializeObject(s, new Newtonsoft.Json.Converters.StringEnumConverter()));

            logLines.ToList().ForEach(Console.WriteLine);
        }

        public virtual void Dispose()
        {
            _logHandle?.Dispose();
        }
        
        #endregion

        public IActionContext Create([CallerMemberName] string name = null,
            string contextGroupName = "default")
        {
            var context =  new ActionContext(contextGroupName, name, Settings, Sanitizers);
            context.State.SetParam("RunnerType", this.GetType().Name);
            
            if (context.Info.IsRoot)
            {
                OnStart?.Invoke(context);
            }
            
            context.OnDispose = c =>
            {
                OnEnd?.Invoke(c);
            };

            return context;
        }

        [Obsolete("Please use CreateAndWrapActionExceptions as its use is clearer.", false)]
        public void RunAction(Action<IActionContext> action, [CallerMemberName] string name = null,
            string contextGroupName = "default")
        {
            CreateAndAppendToActionExceptions(action, name, contextGroupName);
        }


        [Obsolete("Please use CreateAndWrapActionExceptions as its use is clearer.", false)]
        public T RunAction<T>(Func<IActionContext, T> action, [CallerMemberName] string name = null,
            string contextGroupName = "default")
        {
            return CreateAndAppendToActionExceptions(action, name, contextGroupName);
        }

        [Obsolete("Please use CreateAndAppendToActionExceptions as its use is clearer.", false)]
        public async Task RunActionAsync(Func<IActionContext, Task> action, [CallerMemberName] string name = null,
            string contextGroupName = "default")
        {
            await CreateAndAppendToActionExceptionsAsync(action, name, contextGroupName);
        }

        [Obsolete("Please use CreateAndAppendToActionExceptions as its use is clearer.", false)]
        public async Task<T> RunActionAsync<T>(Func<IActionContext, Task<T>> action, [CallerMemberName] string name = null,
            string contextGroupName = "default")
        {
            return await CreateAndAppendToActionExceptionsAsync(action, name, contextGroupName);
        }

        public void CreateAndAppendToActionExceptions(Action<IActionContext> action,
            [CallerMemberName] string name = null, string contextGroupName = "default")
        {
            CreateAndAppendToActionExceptions(action, HandleError, name, contextGroupName);
        }
        
        public void CreateAndAppendToActionExceptions(Action<IActionContext> action, Func<Exception, IActionContext, Exception> errorHandlingOverride, [CallerMemberName]string name = null, string contextGroupName = "default")
        {
            if (action == null) return;

            using var context = new ActionContext(contextGroupName, name, Settings, Sanitizers);
            context.State.SetParam("RunnerType", this.GetType().Name);
            
            try
            {
                if (context.Info.IsRoot)
                {
                    OnStart?.Invoke(context);
                }

                action.Invoke(context);
                    
                OnEnd?.Invoke(context);
            }
            catch (Exception ex)
            {
                var handleError = errorHandlingOverride ?? HandleError;
                throw handleError(ex, context);
            }
        }

        public T CreateAndAppendToActionExceptions<T>(Func<IActionContext, T> action, [CallerMemberName] string name = null,
            string contextGroupName = "default")
        {
            return CreateAndAppendToActionExceptions(action, HandleError, name, contextGroupName);
        }
        
        public T CreateAndAppendToActionExceptions<T>(Func<IActionContext, T> action, Func<Exception, IActionContext, Exception> errorHandlingOverride, [CallerMemberName]string name = null, string contextGroupName = "default")
        {
            if (action == null) return default;

            using var context = new ActionContext(contextGroupName, name, Settings, Sanitizers);
            context.State.SetParam("RunnerType", this.GetType().Name);
            
            try
            {
                if (context.Info.IsRoot)
                {
                    OnStart?.Invoke(context);
                }

                var result = action.Invoke(context);
                    
                OnEnd?.Invoke(context);

                return result;
            }
            catch (Exception ex)
            {
                var handleError = errorHandlingOverride ?? HandleError;
                throw handleError(ex, context);
            }
        }

        public async Task CreateAndAppendToActionExceptionsAsync(Func<IActionContext, Task> action, [CallerMemberName] string name = null,
            string contextGroupName = "default")
        {
            await CreateAndAppendToActionExceptionsAsync(action, HandleError, name, contextGroupName);
        }

        public async Task CreateAndAppendToActionExceptionsAsync(Func<IActionContext, Task> action, Func<Exception, IActionContext, Exception> errorHandlingOverride, [CallerMemberName]string name = null, string contextGroupName = "default")
        {
            if (action == null) return;

            using var context = new ActionContext(contextGroupName, name, Settings, Sanitizers);
            context.State.SetParam("RunnerType", this.GetType().Name);
            
            try
            {
                if (context.Info.IsRoot)
                {
                    OnStart?.Invoke(context);
                }

                await action.Invoke(context);
                    
                OnEnd?.Invoke(context);
            }
            catch (Exception ex)
            {
                var handleError = errorHandlingOverride ?? HandleError;
                throw handleError(ex, context);
            }
        }

        public async Task<T> CreateAndAppendToActionExceptionsAsync<T>(Func<IActionContext, Task<T>> action, [CallerMemberName] string name = null,
            string contextGroupName = "default")
        {
            return await CreateAndAppendToActionExceptionsAsync(action, HandleError, name, contextGroupName);
        }

        public async Task<T> CreateAndAppendToActionExceptionsAsync<T>(Func<IActionContext, Task<T>> action, Func<Exception, IActionContext, Exception> errorHandlingOverride, [CallerMemberName]string name = null, string contextGroupName = "default")
        {
            if (action == null) return default;
            
            using var context = new ActionContext(contextGroupName, name, Settings, Sanitizers);
            context.State.SetParam("RunnerType", this.GetType().Name);
            
            try
            {
                if (context.Info.IsRoot)
                {
                    OnStart?.Invoke(context);
                }

                var result = await action.Invoke(context);
                    
                OnEnd?.Invoke(context);

                return result;
            }
            catch (Exception ex)
            {
                var handleError = errorHandlingOverride ?? HandleError;
                throw handleError(ex, context);
            }
        }

        private Exception HandleError(Exception ex, IActionContext context)
        {
            var wasHandled = ex?.Data.Contains("ContextExceptionHandled");

            if (ex == null || wasHandled == true) return ex;
            
            context.State.SetParam("Exception", ex);

            context.Logger.Log(Settings.ContextErrorMessageLevel,
                $"An exception of type {ex.GetType().Name} was thrown within the context '{context.Info.ContextName}'!");

            ex.Data.Add("ContextExceptionHandled", true);
            ex.Data.Add("ContextParams", context.State.Params);
            ex.Data.Add("ContextEntries", context.Logger.LogEntries.ToArray());

            context.Logger.ErrorToEmit = ex;

            return ex;
        }
    }
}
