﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContextRunner.Samples.Web.Models;
using Microsoft.AspNetCore.Mvc;
using NLog;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ContextRunner.Samples.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SurveyController : ControllerBase
    {
        private readonly IContextRunner _runner;

        public SurveyController(IContextRunner runner = null)
        {
            _runner = runner; //?? ActionContextRunner.Runner;
        }

        [HttpPost]
        public async Task<SurveyRequest> Post(SurveyRequest survey)
        {
            return await _runner.CreateAndAppendToActionExceptions(async context =>
            {
                context.Logger.Debug("Simulating saving survey information...");
                context.State.SetParam("Survey", survey);

                var logger = LogManager.LogFactory.GetCurrentClassLogger();
                logger.Debug("This is a test");

                return survey;
            }, "SurveyService");
        }
        
        [HttpPost]
        [Route("deprecated")]
        public async Task<SurveyRequest> Post2(SurveyRequest survey)
        {
            return await _runner.RunAction(async context =>
            {
                context.Logger.Debug("Simulating saving survey information...");
                context.State.SetParam("Survey", survey);

                var logger = LogManager.LogFactory.GetCurrentClassLogger();
                logger.Debug("This is a test w/ deprecated method");

                return survey;
            }, "SurveyService");
        }

        [HttpPut]
        public async Task<SurveyRequest> Put(SurveyRequest survey)
        {
            using var context = _runner.Create("SurveyService");

            context.Logger.Debug("Pretending we're updating the survey information...");
            context.State.SetParam("Survey", survey);

            var logger = LogManager.LogFactory.GetCurrentClassLogger();
            logger.Debug("This is yet another test");

            return survey;
        }

        [HttpPost]
        [Route("nested")]
        public async Task<SurveyRequest> PostNested(SurveyRequest survey)
        {
            return await _runner.RunAction(async context =>
            {
                context.Logger.Debug("Simulating saving survey information...");
                context.State.SetParam("Survey", survey);

                await _runner.RunAction(async context =>
                {
                    context.Logger.Debug("This is a nested wrapper...");
                    for (var x = 0; x < 5; x++)
                    {
                        await _runner.RunAction(async context =>
                        {
                            context.Logger.Debug($"this is service {x} inside of the nested wrapper...");
                            context.State.SetParam($"Survey{x}", survey);

                            return survey;
                        }, $"NestedService{x}", "nested");
                    }
                }, $"WrapperService", "nested");

                return survey;
            }, "SurveyService");
        }

        [HttpPost]
        [Route("inverted")]
        public async Task<SurveyRequest> PostNestedInverted(SurveyRequest survey)
        {
            return await _runner.RunAction(async context =>
            {
                context.Logger.Debug("Simulating saving survey information...");
                context.State.SetParam("Survey", survey);

                await _runner.RunAction(async context =>
                {
                    context.Logger.Debug("This is a nested wrapper...");
                    for (var x = 0; x < 5; x++)
                    {
                        await _runner.RunAction(async context =>
                        {
                            context.Logger.Debug($"this is service {x} inside of the nested wrapper...");
                            context.State.SetParam($"Survey{x}", survey);

                            return survey;
                        }, $"NestedService{x}");
                    }
                }, $"WrapperService");

                return survey;
            }, "SurveyService", "nested");
        }
    }
}
