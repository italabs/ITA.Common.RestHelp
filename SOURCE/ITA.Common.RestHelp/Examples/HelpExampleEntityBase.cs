using System;
using System.Collections.Generic;
using ITA.Common.RestHelp.Interfaces;
using Newtonsoft.Json;

namespace ITA.Common.RestHelp.Examples
{
    public abstract class HelpExampleEntityBase<T> : IHelpExampleEntityPresenter
    {
        protected readonly Dictionary<HelpExampleType, string> Examples;

        public HelpExampleEntityBase()
        {
            Examples = new Dictionary<HelpExampleType, string>();
        }        

        #region Implementation of IHelpExampleEntityPresenter

        public Type TargetType { get { return typeof(T); } }

        public virtual Dictionary<HelpExampleType, string> GetExamples()
        {
            return Examples;
        }
        
        #endregion

        protected virtual void AddExample(HelpExampleType exampleType, T entity)
        {
            AddExample(exampleType, JsonConvert.SerializeObject(entity, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }

        protected virtual void AddExample(HelpExampleType exampleType, string jsonEntity)
        {
            Examples.Add(exampleType, jsonEntity);
        }
    }
}