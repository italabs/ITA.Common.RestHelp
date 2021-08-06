using System;
using System.Collections.Generic;
using ITA.Common.RestHelp.Examples;

namespace ITA.Common.RestHelp.Interfaces
{
    public class HelpExampleProvider : IHelpExampleProvider
    {        
        protected Dictionary<string, string> _exmaples = new Dictionary<string, string>();

        #region Implementation of IHelpExampleProvider

        public string GetExample(HelpExampleType exampleType, Type entityType)
        {
            var key = GetKey(exampleType, entityType);
            return _exmaples.ContainsKey(key) ? _exmaples[key] : null;
        }

        #endregion

        public void Register(IHelpExampleEntityPresenter presenter)
        {
            var type = presenter.TargetType;
            foreach (var item in presenter.GetExamples())
            {
                var key = GetKey(item.Key, type);
                if (_exmaples.ContainsKey(key))
                    throw new Exception(string.Format("Example presenter '{0}' for entity type '{1}' already exists.", item.Key, presenter.TargetType.Name));

                _exmaples.Add(key, item.Value);
            }
            
        }

        private static string GetKey(HelpExampleType exampleType, Type targetType)
        {
            return string.Format("{0}-{1}", exampleType, targetType.FullName);
        }
    }
}