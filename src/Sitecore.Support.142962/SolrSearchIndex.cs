
namespace Sitecore.Support.ContentSearch.SolrProvider
{
    using System.Collections.Generic;
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.Events;
    using Sitecore.ContentSearch.Maintenance;

    public class SolrSearchIndex : Sitecore.ContentSearch.SolrProvider.SolrSearchIndex
    {
        public SolrSearchIndex(string name, string core, IIndexPropertyStore propertyStore, string @group)
            : base(name, core, propertyStore, @group)
        {
        }

        public SolrSearchIndex(string name, string core, IIndexPropertyStore propertyStore)
            : base(name, core, propertyStore)
        {
        }

        public override void Update(IEnumerable<IIndexableUniqueId> indexableUniqueIds)
        {
            this.PerformUpdate(indexableUniqueIds, IndexingOptions.Default);
        }

        public override void Update(IEnumerable<IIndexableUniqueId> indexableUniqueIds, IndexingOptions indexingOptions)
        {
            this.PerformUpdate(indexableUniqueIds, indexingOptions);
        }

        private void PerformUpdate(IEnumerable<IIndexableUniqueId> indexableUniqueIds, IndexingOptions indexingOptions)
        {
            if (!this.ShouldStartIndexing(indexingOptions))
                return;

            var events = this.Locator.GetInstance<Sitecore.Abstractions.IEvent>();

            events.RaiseEvent("indexing:start", this.Name, false);
            this.Locator.GetInstance<Sitecore.Abstractions.IEventManager>().QueueEvent(new IndexingStartedEvent { IndexName = this.Name, FullRebuild = false });

            using (var context = this.CreateUpdateContext())
            {
                if (context.IsParallel)
                {
                    this.ParallelForeachProxy.ForEach(indexableUniqueIds, context.ParallelOptions, uniqueId =>
                    {
                        if (!this.ShouldStartIndexing(indexingOptions))
                            return;

                        foreach (var crawler in this.Crawlers)
                        {
                            crawler.Update(context, uniqueId, indexingOptions);
                        }
                    });

                    if (!this.ShouldStartIndexing(indexingOptions))
                    {
                        context.Commit();
                        return;
                    }
                }
                else
                {
                    foreach (var uniqueId in indexableUniqueIds)
                    {
                        if (!this.ShouldStartIndexing(indexingOptions))
                        {
                            context.Commit();
                            return;
                        }
                        foreach (var crawler in this.Crawlers)
                        {
                            crawler.Update(context, uniqueId, indexingOptions);
                        }
                    }
                }

                context.Commit();
            }

            events.RaiseEvent("indexing:end", this.Name, false);
            this.Locator.GetInstance<Sitecore.Abstractions.IEventManager>().QueueEvent(new IndexingFinishedEvent { IndexName = this.Name, FullRebuild = false });
        }
    }
}