using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Test;
using Opc.Ua.Server;

namespace OPC.Server
{
    internal class ChangeValue
    {
        private DataGenerator m_generator;
        private IServerInternal Server;

        public ChangeValue(IServerInternal server)
        {
            Server = server;
        }

        public object RandmValue(BaseVariableState v)
        {
            if (m_generator == null)
            {
                m_generator = new DataGenerator(null);
                m_generator.BoundaryValueFrequency = 0;
            }

            object value = null;

            while (value == null)
            {
                value = m_generator.GetRandom(v.DataType, v.ValueRank, new uint[] { 10 }, Server.TypeTree);
            }

            return value;
        }
    }
}