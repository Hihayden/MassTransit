// Copyright 2007-2016 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.RabbitMqTransport.Pipeline
{
    using System.Threading.Tasks;
    using Contexts;
    using MassTransit.Pipeline;
    using RabbitMQ.Client;
    using Util;


    /// <summary>
    /// Creates a receiving model context using the connection
    /// </summary>
    public class ReceiveModelFilter :
        IFilter<ConnectionContext>
    {
        readonly IPipe<ModelContext> _pipe;
        readonly ITaskSupervisor _supervisor;
        readonly ModelSettings _settings;

        public ReceiveModelFilter(IPipe<ModelContext> pipe, ITaskSupervisor supervisor, ModelSettings settings)
        {
            _pipe = pipe;
            _supervisor = supervisor;
            _settings = settings;
        }

        void IProbeSite.Probe(ProbeContext context)
        {
        }

        async Task IFilter<ConnectionContext>.Send(ConnectionContext context, IPipe<ConnectionContext> next)
        {
            using (var scope = _supervisor.CreateScope($"{TypeMetadataCache<ReceiveModelFilter>.ShortName}"))
            {
                IModel model = await context.CreateModel().ConfigureAwait(false);

                using (var modelContext = new RabbitMqModelContext(context, model, scope, _settings))
                {
                    await _pipe.Send(modelContext).ConfigureAwait(false);
                }

                await scope.Completed.ConfigureAwait(false);

                await next.Send(context).ConfigureAwait(false);
            }
        }
    }
}