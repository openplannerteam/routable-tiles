using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RouteableTiles.IO.JsonLD
{
    /// <summary>
    /// A json-writer.
    /// </summary>
    public class JsonWriter
    {
        private readonly TextWriter _writer;
        private readonly Stack<Status> _statusStack;

        /// <summary>
        /// Creates a new json writer.
        /// </summary>
        public JsonWriter(TextWriter writer)
        {
            _writer = writer;
            _statusStack = new Stack<Status>();
        }

        /// <summary>
        /// Writes the object open char.
        /// </summary>
        public async Task WriteOpenAsync()
        {
            if (_statusStack.Count > 0)
            {
                var status = _statusStack.Peek();

                if (status == Status.ArrayValueWritten)
                {
                    await _writer.WriteAsync(',');
                }
            }

            _statusStack.Push(Status.ObjectOpened);
            await _writer.WriteAsync('{');
        }

        /// <summary>
        /// Writes the object close char.
        /// </summary>
        public async Task WriteCloseAsync()
        {
            Status status;
            if (_statusStack.Count == 0)
            {
                throw new Exception("Cannot close object at this point.");
            }

            if (_statusStack.Count > 0)
            {
                status = _statusStack.Peek();

                if (status == Status.PropertyNameWritten)
                {
                    throw new Exception("Cannot close object right after writing a property name.");
                }
            }

            await _writer.WriteAsync('}');
            while (_statusStack.Peek() != Status.ObjectOpened)
            {
                _statusStack.Pop();
            }

            _statusStack.Pop();

            if (_statusStack.Count > 0)
            {
                status = _statusStack.Peek();
                if (status == Status.PropertyNameWritten)
                {
                    // the object was a property value.
                    _statusStack.Push(Status.PropertyValueWritten);
                }

                if (status == Status.ArrayOpenWritten ||
                    status == Status.ArrayValueWritten)
                {
                    // the array was an array value.
                    _statusStack.Push(Status.ArrayValueWritten);
                }
            }
        }

        /// <summary>
        /// Writes a property name.
        /// </summary>
        public async Task WritePropertyNameAsync(string name, bool escape = false)
        {
            if (_statusStack.Count == 0)
            {
                throw new Exception("Cannot write property name at this point.");
            }
            else
            {
                var status = _statusStack.Peek();
                if (status != Status.PropertyValueWritten &&
                    status != Status.ObjectOpened)
                {
                    throw new Exception("Cannot write property name at this point.");
                }

                if (status == Status.PropertyValueWritten)
                {
                    // write comma before starting new property.
                    await _writer.WriteAsync(',');
                }
            }

            await _writer.WriteAsync('"');
            if (escape)
            {
                name = JsonTools.Escape(name);
            }

            await _writer.WriteAsync(name);
            await _writer.WriteAsync('"');
            await _writer.WriteAsync(':');
            _statusStack.Push(Status.PropertyNameWritten);
        }

        /// <summary>
        /// Writes a property value.
        /// </summary>
        public async Task WritePropertyValueAsync(string value, bool useQuotes = false, bool escape = false)
        {
            if (_statusStack.Count == 0)
            {
                throw new Exception("Cannot write property value at this point.");
            }
            else
            {
                var status = _statusStack.Peek();
                if (status != Status.PropertyNameWritten)
                {
                    throw new Exception("Cannot write property value at this point.");
                }
            }

            if (useQuotes) await _writer.WriteAsync('"');
            if (escape)
            {
                value = JsonTools.Escape(value);
            }

            await _writer.WriteAsync(value);
            if (useQuotes) await _writer.WriteAsync('"');
            _statusStack.Push(Status.PropertyValueWritten);
        }

        /// <summary>
        /// Writes a property and it's value.
        /// </summary>
        public async Task WritePropertyAsync(string name, string value, bool useQuotes = false, bool escape = false)
        {
            await this.WritePropertyNameAsync(name, escape);
            await this.WritePropertyValueAsync(value, useQuotes, escape);
        }

        /// <summary>
        /// Writes the array open char.
        /// </summary>
        public async Task WriteArrayOpenAsync()
        {
            if (_statusStack.Count == 0)
            {
                throw new Exception("Cannot open array at this point.");
            }
            else
            {
                var status = _statusStack.Peek();
                if (status != Status.PropertyNameWritten &&
                    status != Status.ArrayOpenWritten &&
                    status != Status.ArrayValueWritten)
                {
                    throw new Exception("Cannot open array at this point.");
                }

                if (status == Status.ArrayValueWritten)
                {
                    _writer.Write(',');
                }
            }

            await _writer.WriteAsync('[');
            _statusStack.Push(Status.ArrayOpenWritten);
        }

        /// <summary>
        /// Writes the array close char.
        /// </summary>
        public async Task WriteArrayCloseAsync()
        {
            Status status;
            if (_statusStack.Count == 0)
            {
                throw new Exception("Cannot open array at this point.");
            }
            else
            {
                status = _statusStack.Peek();
                if (status != Status.ArrayOpenWritten &&
                    status != Status.ArrayValueWritten)
                {
                    throw new Exception("Cannot open array at this point.");
                }
            }

            await _writer.WriteAsync(']');

            status = _statusStack.Peek();
            while (status != Status.ArrayOpenWritten)
            {
                _statusStack.Pop();
                status = _statusStack.Peek();
            }

            _statusStack.Pop();


            if (_statusStack.Count > 0)
            {
                status = _statusStack.Peek();
                if (status == Status.PropertyNameWritten)
                {
                    // the array was a property value.
                    _statusStack.Push(Status.PropertyValueWritten);
                }

                if (status == Status.ArrayOpenWritten ||
                    status == Status.ArrayValueWritten)
                {
                    // the array was an array value.
                    _statusStack.Push(Status.ArrayValueWritten);
                }
            }
        }

        /// <summary>
        /// Writes an array value.
        /// </summary>
        public async Task WriteArrayValueAsync(string value, bool useQuotes = false, bool escape = true)
        {
            if (_statusStack.Count == 0)
            {
                throw new Exception("Cannot write array at this point.");
            }
            else
            {
                var status = _statusStack.Peek();
                if (status != Status.ArrayOpenWritten &&
                    status != Status.ArrayValueWritten)
                {
                    throw new Exception("Cannot write array at this point.");
                }

                if (status == Status.ArrayValueWritten)
                {
                    await _writer.WriteAsync(",");
                }
            }

            if (useQuotes) await _writer.WriteAsync('"');
            if (escape)
            {
                value = JsonTools.Escape(value);
            }

            await _writer.WriteAsync(value);
            if (useQuotes) await _writer.WriteAsync('"');
            _statusStack.Push(Status.ArrayValueWritten);
        }

        /// <summary>
        /// Flushes this writer.
        /// </summary>
        public async Task FlushAsync()
        {
            await _writer.FlushAsync();
        }
        
        /// <summary>
        /// Gets the text writer.
        /// </summary>
        /// <returns></returns>
        public TextWriter TextWriter => _writer;

        private enum Status
        {
            ObjectOpened,
            PropertyNameWritten,
            PropertyValueWritten,
            ArrayOpenWritten,
            ArrayValueWritten
        }
    }
}