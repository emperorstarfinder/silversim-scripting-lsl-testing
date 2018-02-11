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

using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using System.Collections;
using System.Collections.Generic;

namespace SilverSim.Scripting.Lsl.Api.Notecards
{
    public sealed partial class NotecardApi
    {
        public sealed class NotecardDataEnumerator : IEnumerator<string>
        {
            private readonly NotecardData m_Notecard;
            private int m_Position = -1;

            public NotecardDataEnumerator(NotecardData notecard)
            {
                m_Notecard = notecard;
            }

            public string Current => m_Notecard[m_Position];

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                /* intentionally left empty */
            }

            public bool MoveNext()
            {
                return ++m_Position < m_Notecard.Count;
            }

            public void Reset()
            {
                m_Position = -1;
            }
        }

        [APILevel(APIFlags.ASSL)]
        [APIDisplayName("notecard")]
        [APIAccessibleMembers("Count")]
        public sealed class NotecardData
        {
            private readonly string[] m_NotecardLines;

            public NotecardData(string[] lines)
            {
                m_NotecardLines = lines;
            }

            public string this[int index] => (m_NotecardLines != null && index >= 0 && index < m_NotecardLines.Length) ? m_NotecardLines[index] : string.Empty;

            public int Count => m_NotecardLines?.Length ?? 0;

            public NotecardDataEnumerator GetLslForeachEnumerator() => new NotecardDataEnumerator(this);
        }

        [APILevel(APIFlags.ASSL, "asGetNotecardLines")]
        [APIExtension(APIExtension.Properties, "Notecard")]
        public NotecardData GetNotecardData(ScriptInstance instance, string name)
        {
            lock (instance)
            {
                UUID assetID = instance.GetNotecardAssetID(name);
                Notecard nc = instance.Part.ObjectGroup.Scene.GetService<NotecardCache>()[assetID];
                return new NotecardData(nc.Text.Split('\n'));
            }
        }
    }
}
