// Copyright 2015 Destructurama Contributors, Serilog Contributors
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Destructurama.Attributed
{
    [AttributeUsage(AttributeTargets.Property)]
    public class LogMaskedAttribute : Attribute
    {
        private const string DefaultMask = "***";

        public LogMaskedAttribute(string Text = DefaultMask, int ShowFirst = 0, int ShowLast = 0, bool PreserveLength = false)
        {
            this.Text = Text;
            this.ShowFirst = ShowFirst;
            this.ShowLast = ShowLast;
            this.PreserveLength = PreserveLength;
        }

        /// <summary>
        /// Check to see if custom Text has been provided.
        /// If true PreserveLength is ignored.
        /// </summary>
        /// <returns></returns>
        internal bool IsDefaultMask()
        {
            return Text == DefaultMask;
        }

        public string Text { get; }
        public int ShowFirst { get; }
        public int ShowLast { get; }
        public bool PreserveLength { get; }
        public bool PreserveWhitespace { get; }
    }
}
