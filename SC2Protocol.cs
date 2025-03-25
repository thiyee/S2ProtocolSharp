using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using System.Linq;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Runtime;

namespace SC2Protocol
{
    public abstract class PyEnumerableObject : IEnumerable<object>
    {
        public abstract IEnumerator<object> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public abstract object this[object index] { get; set; }
    }
    public class PyList : PyEnumerableObject

    {
        private List<object> objects;

        public PyList()
        {
            objects = new List<object>();
        }

        public void Add(object item)
        {
            objects.Add(item);
        }

        public override object this[object index]
        {
            get
            {
                if (index is long bigIndex)
                {
                    if (bigIndex >= int.MinValue && bigIndex <= int.MaxValue)
                    {
                        return objects[(int)bigIndex];
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("index", "long value is out of range for an int.");
                    }
                }

                if (index is int intIndex)
                {
                    return objects[intIndex];
                }

                throw new ArgumentException("Index must be either int or long.");
            }
            set
            {
                if (index is long bigIndex)
                {
                    if (bigIndex >= int.MinValue && bigIndex <= int.MaxValue)
                    {
                        objects[(int)bigIndex] = value;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("index", "long value is out of range for an int.");
                    }
                }
                else if (index is int intIndex)
                {
                    objects[intIndex] = value;
                }
                else
                {
                    throw new ArgumentException("Index must be either int or long.");
                }
            }
        }


        public int Length => objects.Count;
        public int Count => objects.Count;


        public void PrintList()
        {
            foreach (var item in objects)
            {
                Console.Write(item + " ");
            }
            Console.WriteLine();
        }
        public override string ToString()
        {
            string result = "";
            foreach (object o in objects)
            {
                result += $"{o},";
            }
            result = result.Remove(result.Length - 1, 1);
            return $"[{this.GetType().Name}] [{result}]";
        }

        public override IEnumerator<object> GetEnumerator()
        {
            return objects.GetEnumerator();
        }
    }
    public class PyTuple : PyEnumerableObject

    {
        private List<object> objects;

        public PyTuple(IEnumerable<object> items)
        {
            objects = new List<object>(items);
        }
        public PyTuple(params object[] items)
        {
            objects = items.ToList();
        }

        public override object this[object index]
        {
            get => objects[(int)index];
            set => objects[(int)index] = value;
        }

        public int Length => objects.Count;

        public void Add(object item)
        {
            objects.Add(item);
        }

        public void RemoveAt(int index)
        {
            objects.RemoveAt(index);
        }

        public void PrintTuple()
        {
            foreach (var item in objects)
            {
                Console.Write(item + " ");
            }
            Console.WriteLine();
        }
        public override string ToString()
        {
            string result = "";
            foreach (object o in objects)
            {
                result += $"{o},";
            }
            result = result.Remove(result.Length - 1, 1);
            return $"[{this.GetType().Name}] ({result})";
        }

        public override IEnumerator<object> GetEnumerator()
        {
            return objects.GetEnumerator();
        }
    }
    public class PyDictionary : PyEnumerableObject

    {
        private Dictionary<object, object> dict;
        public int Count => dict.Count;
        public IEnumerable<object> Keys => dict.Keys;
        public IEnumerable<object> Values => dict.Values;
        public PyDictionary()
        {
            dict = new Dictionary<object, object>();
        }

        public void Add(object key, object value)
        {
            dict[key] = value;
        }

        public override object this[object key]
        {
            get => dict[key];
            set => dict[key] = value;
        }


        public void PrintDictionary()
        {
            foreach (var kvp in dict)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value}");
            }
        }
        public override string ToString()
        {
            string result = "";
            foreach (var o in dict)
            {
                result += $"{o.Key}:{o.Value},";
            }
            result = result.Remove(result.Length - 1, 1);
            return $"[{this.GetType().Name}] {{{result}}}";
        }
        public override IEnumerator<object> GetEnumerator()
        {
            return dict.Keys.GetEnumerator();
        }

        internal bool ContainsKey(object tag)
        {
            return dict.ContainsKey(tag);
        }
    }
    public enum BaseType
    {
        _array,
        _bitarray,
        _blob,
        _bool,
        _choice,
        _fourcc,
        _int,
        _null,
        _optional,
        _real32,
        _real64,
        _struct,
    }
    class Parser
    {
        List<Token> Tokens;
        public Parser(string code)
        {
            Tokens = new Lexer(code).Run();

        }
        public enum SyntaxKind
        {
            SingleLineComment,
            NumericLiteral,
            StringLiteral,
            OpenBraceToken, // '{' 符号，表示代码块开始
            CloseBraceToken, // '}' 符号，表示代码块结束
            OpenParenToken, // '(' 符号，表示参数列表开始
            CloseParenToken, // ')' 符号，表示参数列表结束
            OpenBracketToken, // '[' 符号，表示数组或索引访问开始
            CloseBracketToken, // ']' 符号，表示数组或索引访问结束
            CommaToken, // ',' 符号，用于分隔列表项
            MinusToken, // '-' 符号，减法
            ColonToken, //':'符号，字典

            ListExpression,
            TupleExpression,
            DictionaryExpression
        }
        public class Token
        {
            public SyntaxKind Kind;
            public string Literal { get; set; }
            public object AnalyticValue;

            public Token(SyntaxKind Kind, string Literal)
            {
                this.Kind = Kind;
                this.Literal = Literal;
            }
            public Token(SyntaxKind Kind, string Literal, object AnalyticValue)
            {
                this.Kind = Kind;
                this.Literal = Literal;
                this.AnalyticValue = AnalyticValue;
            }

            public override string ToString()
            {
                return $"{Kind}: {Literal}";
            }
        }
        public class Lexer
        {
            string SourceCode;
            int Position;

            public static readonly Dictionary<string, SyntaxKind> TextToTokenTable = new Dictionary<string, SyntaxKind> {
        {"{", SyntaxKind.OpenBraceToken},
        {"}", SyntaxKind.CloseBraceToken},
        {"(", SyntaxKind.OpenParenToken},
        {")", SyntaxKind.CloseParenToken},
        {"[", SyntaxKind.OpenBracketToken},
        {"]", SyntaxKind.CloseBracketToken},
        {",", SyntaxKind.CommaToken},
        {"-", SyntaxKind.MinusToken},
        {":", SyntaxKind.ColonToken},
        };

            public Lexer(string code)
            {
                SourceCode = code;
            }
            public List<Token> Run()
            {
                List<Token> result = new List<Token>();

                Token ReadString()
                {
                    int PeekPosition = Position;
                    string Literal = "";
                    PeekPosition++;//skip first '
                    while (SourceCode[PeekPosition] != '\'')
                    {
                        Literal += SourceCode[PeekPosition];
                        PeekPosition++;
                    }
                    Position = PeekPosition;
                    return new Token(SyntaxKind.StringLiteral, $"'{Literal}'", Literal);
                }
                Token ReadNumeric(bool negative = false)
                {
                    int PeekPosition = Position;
                    string Literal = "";
                    if (negative)
                    {
                        PeekPosition++;
                        Literal = "-";
                    }
                    while (SourceCode[PeekPosition] >= '0' && SourceCode[PeekPosition] <= '9')
                    {
                        Literal += SourceCode[PeekPosition];
                        PeekPosition++;
                    }
                    ;
                    var Value = long.Parse(Literal);
                    Position = PeekPosition - 1;
                    return new Token(SyntaxKind.NumericLiteral, Literal, Value);
                }
                Token ReadComment()
                {
                    int PeekPosition = Position;
                    string Literal = "";
                    PeekPosition++;//skip #
                    while (PeekPosition < SourceCode.Length && SourceCode[PeekPosition] != '\n')
                    {
                        Literal += SourceCode[PeekPosition];
                        PeekPosition++;
                    }
                    Position = PeekPosition;
                    return new Token(SyntaxKind.SingleLineComment, $"#{Literal}", Literal);
                }
                for (Position = 0; Position < SourceCode.Length; Position++)
                {
                    char c = SourceCode[Position];

                    switch (c)
                    {
                        case ' ':
                            break;
                        case '\'':
                            result.Add(ReadString());
                            break;
                        case '#':
                            result.Add(ReadComment());
                            break;
                        case '{':
                        case '}':
                        case '(':
                        case ')':
                        case '[':
                        case ']':
                        case ',':
                        case ':':
                            result.Add(new Token(TextToTokenTable[c.ToString()], c.ToString()));
                            break;
                        case '-':
                            result.Add(ReadNumeric(true));
                            break;
                        case char n when (n >= '0' && n <= '9'):
                            result.Add(ReadNumeric());
                            break;
                    }
                }
                return result;
            }
        }
        public PyEnumerableObject Parse()
        {
            PyEnumerableObject pyObject = null;
            Stack<PyEnumerableObject> objectStack = new Stack<PyEnumerableObject>();
            object key = null;  
            object value = null;
            for (int i = 0; i < Tokens.Count; i++)
            {
                var token = Tokens[i];
                switch (token.Kind)
                {
                    case SyntaxKind.OpenBracketToken:
                        // Start a new list
                        var pyList = new PyList();
                        objectStack.Push(pyList);
                        break;
                    case SyntaxKind.OpenParenToken:
                        // Start a new tuple
                        var pyTuple = new PyTuple(new List<object>());
                        objectStack.Push(pyTuple);
                        break;
                    case SyntaxKind.OpenBraceToken:
                        // Start a new dictionary
                        var pyDict = new PyDictionary();
                        objectStack.Push(pyDict);
                        break;
                    case SyntaxKind.CloseBraceToken:
                    case SyntaxKind.CloseBracketToken:
                    case SyntaxKind.CloseParenToken:
                        pyObject = objectStack.Pop();
                        if (objectStack.Count > 0)
                        {
                            switch (objectStack.Peek())
                            {
                                case PyTuple t:
                                    t.Add(pyObject);
                                    break;
                                case PyList l:
                                    l.Add(pyObject);
                                    break;
                                case PyDictionary dic:
                                    if (key == null)
                                        key = pyObject;
                                    else
                                        value = pyObject;
                                    if (key != null && value != null)
                                    {
                                        dic.Add(key, value);
                                        key = value = null;
                                    }
                                    break;
                            }
                        }
                        break;
                    case SyntaxKind.CommaToken:
                        // Handle comma, continue adding to current container
                        break;

                    case SyntaxKind.ColonToken:
                        break;

                    case SyntaxKind.NumericLiteral:
                    case SyntaxKind.StringLiteral:
                        if (objectStack.Count > 0)
                        {
                            switch (objectStack.Peek())
                            {
                                case PyTuple t:
                                    t.Add(token.AnalyticValue);
                                    break;
                                case PyList l:
                                    l.Add(token.AnalyticValue);
                                    break;
                                case PyDictionary dic:
                                    if (key == null)
                                        key = token.AnalyticValue;
                                    else
                                        value = token.AnalyticValue;
                                    if (key != null && value != null)
                                    {
                                        dic.Add(key, value);
                                        key = value = null;
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }

            // If the objectStack has something left, it should be the final object.
            if (objectStack.Count > 0)
            {
                pyObject = objectStack.Pop();
            }

            return pyObject;
        }
    }
    public class BitPackedBuffer
    {
        private byte[] _data;
        private long _usedbytes;
        private long? _next;
        private long _nextBits;
        private bool _bigEndian;

        public BitPackedBuffer(byte[] contents, string endian = "big")
        {
            _data = contents ?? Array.Empty<byte>();
            _usedbytes = 0;
            _next = null;
            _nextBits = 0;
            _bigEndian = (endian == "big");
        }
        public BitPackedBuffer(byte[] contents, long BitPosition, string endian = "big")
        {
            _data = contents ?? Array.Empty<byte>();
            _usedbytes = 0;
            _next = null;
            _nextBits = 0;
            _bigEndian = (endian == "big");
        }
        public override string ToString()
        {
            string s = (_usedbytes < _data.Length) ? _data[_usedbytes].ToString("X2") : "--";
            return $"buffer({(_nextBits != 0 ? (_next ?? 0) : 0):X2}/{_nextBits},[{_usedbytes}]={s})";
        }
        public bool done()
        {
            return _nextBits == 0 && _usedbytes >= _data.Length;
        }
        public long used_bits()
        {
            return _usedbytes * 8 - _nextBits;
        }
        public void byte_align()
        {
            _nextBits = 0;
        }
        public byte[] read_aligned_bytes(long bytes)
        {
            byte_align();
            if (_usedbytes + bytes > _data.Length)
                throw new InvalidOperationException("Buffer is truncated.");

            var result = new byte[bytes];
            Array.Copy(_data, _usedbytes, result, 0, bytes);
            _usedbytes += bytes;
            return result;
        }
        public long read_bits(long bits)
        {
            long result = 0;
            long resultBits = 0;

            while (resultBits != bits)
            {
                if (_nextBits == 0)
                {
                    if (done())
                    {
                        throw new InvalidOperationException("Buffer is truncated.");
                    }

                    _next = _data[_usedbytes++];
                    _nextBits = 8; 
                }

                var copyBits = Math.Min(bits - resultBits, _nextBits);
                var copy = (_next.Value & ((1L << (int)copyBits) - 1));

                if (_bigEndian)
                {
                    result |= copy << (int)(bits - resultBits - copyBits);
                }
                else
                {
                    result |= copy << (int)resultBits;
                }

                _next >>= (int)copyBits;
                _nextBits -= copyBits;
                resultBits += copyBits;
            }

            return result;
        }
        public byte[] read_unaligned_bytes(long bytes)
        {
            if (bytes > int.MaxValue)
                throw new InvalidOperationException("Length OutofRange.");
            int lbytes = (int)bytes;

            var result = new byte[lbytes];
            for (int i = 0; i < lbytes; i++)
            {
                result[i] = (byte)read_bits(8);
            }
            return result;
        }

        public void skip_aligned_bytes(int bytes)
        {
            byte_align();
            if (_usedbytes + bytes > _data.Length)
            {
                throw new InvalidOperationException("Buffer is truncated.");
            }
            _usedbytes += bytes; 
        }
        public void skip_unaligned_bytes(int bytes)
        {
            skip_bits(bytes * 8);
        }
        public void skip_bits(long bits)
        {
            long skippedBits = 0;
            while (skippedBits != bits)
            {
                if (_nextBits == 0)
                {
                    if (done())
                    {
                        throw new InvalidOperationException("Buffer is truncated.");
                    }
                    _next = _data[_usedbytes++];
                    _nextBits = 8;
                }

                long copyBits = Math.Min(bits - skippedBits, _nextBits);

                _next >>= (int)copyBits;
                _nextBits -= copyBits;
                skippedBits += copyBits;
            }
        }

        public void seek(long bitPosition)
        {
            if (bitPosition < 0 || bitPosition > _data.Length * 8)
            {
                throw new ArgumentOutOfRangeException(nameof(bitPosition), "Bit position is out of range.");
            }

            long byteIndex = bitPosition / 8;
            long bitOffset = bitPosition % 8;

            _usedbytes = byteIndex;

            if (bitOffset == 0)
            {
                _next = null;
                _nextBits = 0;
            }
            else
            {
                _next = _data[byteIndex] >> (int)bitOffset;
                _nextBits = 8 - bitOffset;
            }
        }

    }
    public class BitPackedDecoder
    {
        private BitPackedBuffer _buffer;
        private PyList _typeinfos;
        Func<PyEnumerableObject, object>[] QuickMethodCache;
        private BitPackedDecoder self => this;
        public BitPackedDecoder(byte[] contents, PyList typeInfos)
        {
            _buffer = new BitPackedBuffer(contents);
            _typeinfos = typeInfos;
            QuickMethodCache = new Func<PyEnumerableObject, object>[]
            {
                args => _array((PyTuple)args[0], (long)args[1]),
                args => _bitarray((PyTuple)args[0]),
                args => _blob((PyTuple)args[0]),
                args => _bool(),
                args => _choice((PyTuple)args[0], (PyDictionary)args[1]),
                args => _fourcc(),
                args => _int((PyTuple)args[0]),
                args => _null(),
                args => _optional((long)args[0]),
                args => _real32(),
                args => _real64(),
                args => _struct((PyList)args[0])
            };
        }
        public override string ToString()
        {
            return _buffer.ToString();
        }
        public object instance(long typeId)
        {
            //if (typeId >= _typeinfos.Count)
            //{
            //    throw new InvalidOperationException("Invalid type ID.");
            //}
            var typeInfo = (PyEnumerableObject)_typeinfos[typeId];
            return QuickMethodCache[(int)(BaseType)typeInfo[0]]((PyEnumerableObject)typeInfo[1]);
        }
        public object instance(int typeId) => instance((long)typeId);
        public void byte_align()
        {
            _buffer.byte_align();
        }
        public bool done()
        {
            return _buffer.done();
        }
        public long used_bits()
        {
            return _buffer.used_bits();
        }
        public PyList _array(PyTuple bounds, long typeid)
        {
            var length = self._int(bounds);
            var result = new PyList();
            for (int i = 0; i < length; i++)
            {
                result.Add(self.instance(typeid));
            }
            return result;
        }
        public PyTuple _bitarray(PyTuple bounds)
        {
            var length = self._int(bounds);
            return new PyTuple(length, self._buffer.read_bits(length));
        }
        public byte[] _blob(PyTuple bounds)
        {
            var length = self._int(bounds);
            var result = self._buffer.read_aligned_bytes(length);
            return result;
        }
        public bool _bool()
        {
            return self._int(new PyTuple(0l, 1l)) != 0;
        }
        public PyDictionary _choice(PyTuple bounds, PyDictionary fields)
        {
            var tag = self._int(bounds);
            if (!fields.ContainsKey(tag))
                throw new Exception("CorruptedError");
            var field = (PyEnumerableObject)fields[tag];
            return new PyDictionary() { { field[0], self.instance((long)field[1]) } };
        }
        public byte[] _fourcc()
        {
            return self._buffer.read_unaligned_bytes(4);
        }
        public long _int(PyTuple bounds){
            return (long)bounds[0] + self._buffer.read_bits((long)bounds[1]);
        }
        public object _null()
        {
            return null;
        }
        public object _optional(long typeid)
        {
            var exists = self._bool();
            if (exists)
                return self.instance(typeid);
            else return null;
        }
        public float _real32()
        {
            return BitConverter.ToSingle(self._buffer.read_unaligned_bytes(4), 0);
        }
        public float _real64()
        {
            return BitConverter.ToSingle(self._buffer.read_unaligned_bytes(8), 0);
        }
        public PyDictionary _struct(PyList fields)
        {
            PyDictionary result = new PyDictionary();
            foreach (PyEnumerableObject field in fields)
            {
                //if (field[0] is string s && s == "__parent")
                //{
                //    var parent = self.instance((long)field[1]);
                //    if (parent is PyDictionary parentDict)
                //    {
                //        foreach (var key in parentDict)
                //        {
                //            result[key] = parentDict[key];
                //        }
                //    }
                //    else if (fields.Count == 1)
                //        return parent as PyDictionary ?? new PyDictionary();
                //    else
                //        result[field[0]] = parent;
                //}
                //else
                    result[(string)field[0]] = self.instance((long)field[1]);
            
            }

            return result;
        }
        public void skip_instance(long typeId)
        {
            if (typeId >= _typeinfos.Count)
            {
                throw new InvalidOperationException("Invalid type ID.");
            }

            var typeInfo = (PyEnumerableObject)_typeinfos[typeId];
            BaseType methodName = (BaseType)typeInfo[0];
            var methodArgs = (PyEnumerableObject)typeInfo[1];

            switch (methodName)
            {
                case BaseType._array:
                    var arrayBounds = (PyTuple)methodArgs[0];
                    var typeid = (long)methodArgs[1];
                    var arrayLength = _int(arrayBounds);
                    for (int i = 0; i < arrayLength; i++)
                    {
                        skip_instance(typeid); 
                    }
                    break;

                case BaseType._bitarray:
                    var bitarrayBounds = (PyTuple)methodArgs[0];
                    _buffer.skip_bits((int)_int(bitarrayBounds));
                    break;
                case BaseType._blob:
                    var blobBounds = (PyTuple)methodArgs[0];
                    _buffer.skip_aligned_bytes((int)self._int(blobBounds));
                    break;
                case BaseType._bool:
                    _buffer.skip_bits(1);
                    break;
                case BaseType._choice:
                    var choiceBounds = (PyTuple)methodArgs[0];
                    var choiceFields = (PyDictionary)methodArgs[1];
                    var tag = _int(choiceBounds);
                    if (!choiceFields.ContainsKey(tag))
                    {
                        throw new Exception("CorruptedError");
                    }

                    var selectedField = (PyEnumerableObject)choiceFields[tag];
                    var fieldTypeId1 = (long)selectedField[1];

                    skip_instance(fieldTypeId1);
                    break;

                case BaseType._fourcc:
                    _buffer.skip_unaligned_bytes(4);
                    break;
                case BaseType._int:
                    var intBounds = (PyTuple)methodArgs[0];
                    var max = (long)intBounds[1];
                    _buffer.skip_bits((int)max);
                    break;
                case BaseType._null:
                    break;
                case BaseType._optional:
                    var exists = self._bool();
                    if (exists)
                        self.skip_instance((long)methodArgs[0]);
                    break;
                case BaseType._real32:
                    _buffer.skip_unaligned_bytes(4);
                    break;

                case BaseType._real64:
                    _buffer.skip_unaligned_bytes(8);
                    break;
                case BaseType._struct:
                    var fields = (PyList)methodArgs[0];
                    foreach (PyEnumerableObject field in fields)
                    {
                        if (field[0] is string s && s == "__parent")
                        {
                            var parentTypeId = (long)field[1];
                            skip_instance(parentTypeId); 
                        }
                        else
                        {
                            var fieldTypeId = (long)field[1];
                            skip_instance(fieldTypeId); 
                        }
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unknown type method: {methodName}");
            }
        }

        internal void seek(long v)
        {
            _buffer.seek(v);
        }
    }
    public class VersionedDecoder
    {
        private BitPackedBuffer _buffer;
        private PyList _typeinfos;
        Func<PyEnumerableObject, object>[] QuickMethodCache;
        private VersionedDecoder self => this;
        public VersionedDecoder(byte[] contents, PyList typeinfos)
        {
            self._buffer = new BitPackedBuffer(contents);
            self._typeinfos = typeinfos;
            QuickMethodCache = new Func<PyEnumerableObject, object>[]
            {
                args => _array((PyTuple)args[0], (long)args[1]),
                args => _bitarray((PyTuple)args[0]),
                args => _blob((PyTuple)args[0]),
                args => _bool(),
                args => _choice((PyTuple)args[0], (PyDictionary)args[1]),
                args => _fourcc(),
                args => _int((PyTuple)args[0]),
                args => _null(),
                args => _optional((long)args[0]),
                args => _real32(),
                args => _real64(),
                args => _struct((PyList)args[0])
            };
        }
        public override string ToString()
        {
            return _buffer.ToString();
        }
        public object instance(long typeId)
        {
            if (typeId >= _typeinfos.Count)
            {
                throw new InvalidOperationException("Invalid type ID.");
            }
            var typeInfo = (PyEnumerableObject)_typeinfos[typeId];
            return QuickMethodCache[(int)(BaseType)typeInfo[0]]((PyEnumerableObject)typeInfo[1]);
        }
        public object instance(int typeId) => instance((long)typeId);

        public void byte_align()
        {
            self._buffer.byte_align();
        }
        public bool done()
        {
            return self._buffer.done();
        }
        public long used_bits()
        {
            return self._buffer.used_bits();
        }
        public void _expect_skip(long expected)
        {
            if (self._buffer.read_bits(8) != expected)
                throw new Exception("CorruptedError");
        }
        public long _vint()
        {
            var b = self._buffer.read_bits(8);
            var negative = (b & 1) != 0;
            var result = (b >> 1) & 0x3f;
            var bits = 6;
            while ((b & 0x80) != 0)
            {
                b = self._buffer.read_bits(8);
                result |= (b & 0x7f) << bits;
                bits += 7;
            }
            return negative ? -result : result;
        }
        public PyList _array(PyTuple bounds, long typeid)
        {
            self._expect_skip(0);
            var length = self._vint();
            PyList result = new PyList();
            for (int i = 0; i < length; i++)
                result.Add(self.instance(typeid));
            return result;
        }
        public PyTuple _bitarray(PyTuple bounds)
        {
            self._expect_skip(1);
            var length = self._vint();
            return new PyTuple(length, self._buffer.read_aligned_bytes((length + 7) / 8));
        }
        public byte[] _blob(PyTuple bounds)
        {
            self._expect_skip(2);
            var length = self._vint();
            return self._buffer.read_aligned_bytes(length);
        }
        public bool _bool()
        {
            self._expect_skip(6);
            return self._buffer.read_bits(8) != 0;
        }
        public PyDictionary _choice(PyTuple bounds, PyDictionary fields)
        {
            self._expect_skip(3);
            var tag = self._vint();
            if (!fields.ContainsKey(tag))
            {
                self._skip_instance();
                return new PyDictionary();
            }
            var field = (PyEnumerableObject)fields[tag];
            return new PyDictionary() { { field[0], self.instance((long)field[1]) } };
        }
        public byte[] _fourcc()
        {
            self._expect_skip(7);
            return self._buffer.read_aligned_bytes(4);
        }
        public long _int(PyTuple bounds)
        {
            self._expect_skip(9);
            return self._vint();
        }
        public object _null()
        {
            return null;
        }
        public object _optional(long typeid)
        {
            self._expect_skip(4);
            var exists = self._buffer.read_bits(8) != 0;
            return exists ? self.instance(typeid) : null;
        }
        public float _real32()
        {
            self._expect_skip(7);
            return BitConverter.ToSingle(self._buffer.read_aligned_bytes(4), 0);
        }
        public double _real64()
        {
            self._expect_skip(8);
            return BitConverter.ToDouble(self._buffer.read_aligned_bytes(8), 0);
        }
        public PyDictionary _struct(PyList fields)
        {
            PyDictionary result = new PyDictionary();
            self._expect_skip(5);
            var length = self._vint();
            for (int i = 0; i < length; i++)
            {
                var tag = self._vint();
                PyEnumerableObject field = fields.FirstOrDefault(f => ((dynamic)((PyEnumerableObject)f)[2]) == tag) as PyEnumerableObject;
                if (field != null)
                {
                    if ((dynamic)field[0] == "__parent")
                    {
                        var parent = self.instance((dynamic)field[1]);
                        if (parent is PyDictionary dict)
                        {
                            foreach (var key in dict)
                            {
                                result[key] = dict[key];
                            }
                        }
                        else if (fields.Count == 1)
                        {
                            result = parent;
                        }
                        else
                        {
                            result[field[0]] = parent;
                        }
                    }
                    else
                        result[field[0]] = self.instance((dynamic)field[1]);
                }
                else
                    self._skip_instance();
            }
            return result;
        }
        public void _skip_instance()
        {
            var skip = self._buffer.read_bits(8);
            switch ((int)skip)
            {
                case 0:
                    {
                        var length = self._vint();
                        for (int i = 0; i < length; i++)
                            self._skip_instance();
                    }
                    break;
                case 1:
                    {
                        var length = self._vint();
                        self._buffer.read_aligned_bytes((length + 7) / 8);
                    }
                    break;
                case 2:
                    {
                        var length = self._vint();
                        self._buffer.read_aligned_bytes(length);
                    }
                    break;
                case 3:
                    {
                        var tag = self._vint();
                        self._skip_instance();
                    }
                    break;
                case 4:
                    {
                        var exists = self._buffer.read_bits(8) != 0;
                        if (exists)
                            self._skip_instance();
                    }
                    break;
                case 5:
                    {
                        var length = self._vint();
                        for (int i = 0; i < length; i++)
                        {
                            var tag = self._vint();
                            self._skip_instance();
                        }
                    }
                    break;
                case 6:
                    {
                        self._buffer.read_aligned_bytes(1);
                    }
                    break;
                case 7:
                    {
                        self._buffer.read_aligned_bytes(4);
                    }
                    break;
                case 8:
                    {
                        self._buffer.read_aligned_bytes(8);
                    }
                    break;
                case 9:
                    {
                        self._vint();
                    }
                    break;
            }
        }
    }
    public abstract class S2Protocol
    {
        public PyList typeinfos;
        public PyDictionary game_event_types;
        public PyDictionary message_event_types;
        public PyDictionary tracker_event_types;
        public int game_eventid_typeid;
        public int message_eventid_typeid;
        public int tracker_eventid_typeid;
        public int svaruint32_typeid;
        public int replay_userid_typeid;
        public int replay_header_typeid;
        public int game_details_typeid;
        public int replay_initdata_typeid;
        public static Dictionary<int, S2Protocol> Build;
        public static S2Protocol Latest;
        static S2Protocol()
        {
            Build = new Dictionary<int, S2Protocol>();

            var types = Assembly.GetExecutingAssembly().GetTypes();

            var subclasses = types.Where(t => t.IsSubclassOf(typeof(S2Protocol)) && !t.IsAbstract);

            foreach (var subclass in subclasses){
                var instance = Activator.CreateInstance(subclass) as S2Protocol;
                for(int i = 0; i < instance.typeinfos.Count; i++){
                    PyTuple tuple = instance.typeinfos[i] as PyTuple;
                    tuple[0] = (BaseType)Enum.Parse(typeof(BaseType), (string)tuple[0]); 
                }
            }
            Latest = Build[Build.Keys.Max()];
        }
        public long _varuint32_value(PyDictionary value)
        {
            foreach (var v in value.Values)
            {
                return (long)v;
            }
            return 0;
        }
        public List<PyDictionary> _decode_event_stream(BitPackedDecoder decoder, int eventid_typeid, PyDictionary event_types, bool decode_user_id)
        {
            long gameloop = 0;
            List<PyDictionary> result = new List<PyDictionary>();
            while (!decoder.done())
            {
                var start_bits = decoder.used_bits();
                var delta = _varuint32_value((PyDictionary)decoder.instance(svaruint32_typeid));
                gameloop += delta;
                object userid = null;
                if (decode_user_id)
                    userid = decoder.instance(replay_userid_typeid);
                var eventid = decoder.instance(eventid_typeid);

                var typeid = (event_types[eventid] as PyEnumerableObject)?[0];
                var typename = (event_types[eventid] as PyEnumerableObject)?[1];
                if (typeid is null)
                    throw new Exception($"eventid({eventid}) at {decoder}");
                PyDictionary @event = decoder.instance((dynamic)typeid);
                @event["_event"] = typename;
                @event["_eventid"] = eventid;
                @event["_gameloop"] = gameloop;
                if (decode_user_id)
                    @event["_userid"] = userid;
                decoder.byte_align();
                @event["_bits"] = decoder.used_bits() - start_bits;
                result.Add(@event);
            }
            return result;
        }
        public List<PyDictionary> _decode_event_stream(VersionedDecoder decoder, int eventid_typeid, PyDictionary event_types, bool decode_user_id)
        {
            long gameloop = 0;
            List<PyDictionary> result = new List<PyDictionary>();
            while (!decoder.done())
            {
                var start_bits = decoder.used_bits();
                var delta = _varuint32_value((PyDictionary)decoder.instance(svaruint32_typeid));
                gameloop += delta;
                object userid = null;
                if (decode_user_id)
                    userid = decoder.instance(replay_userid_typeid);
                var eventid = decoder.instance(eventid_typeid);

                var typeid = (event_types[eventid] as PyEnumerableObject)?[0];
                var typename = (event_types[eventid] as PyEnumerableObject)?[1];
                if (typeid is null) 
                    throw new Exception($"eventid({eventid}) at {decoder}");
                PyDictionary @event = decoder.instance((dynamic)typeid);
                @event["_event"] = typename;
                @event["_eventid"] = eventid;
                @event["_gameloop"] = gameloop;
                if (decode_user_id)
                    @event["_userid"] = userid;
                decoder.byte_align();
                @event["_bits"] = decoder.used_bits() - start_bits;
                result.Add(@event);
            }
            return result;
        }
        public List<PyDictionary> decode_replay_game_events(byte[] contents)
        {
            var decoder = new BitPackedDecoder(contents, typeinfos);
            return _decode_event_stream(decoder, game_eventid_typeid, game_event_types, true);
        }
        public List<PyDictionary> decode_replay_message_events(byte[] contents)
        {
            var decoder = new BitPackedDecoder(contents, typeinfos);
            return _decode_event_stream(decoder, message_eventid_typeid, message_event_types, true);
        }
        public List<PyDictionary> decode_replay_tracker_events(byte[] contents)
        {
            var decoder = new VersionedDecoder(contents, typeinfos);
            return _decode_event_stream(decoder, tracker_eventid_typeid, tracker_event_types, false);
        }
        public PyDictionary decode_replay_header(byte[] contents)
        {
            var decoder = new VersionedDecoder(contents, typeinfos);
            return decoder.instance(replay_header_typeid) as PyDictionary;
        }
        public PyDictionary decode_replay_details(byte[] contents)
        {
            var decoder = new VersionedDecoder(contents, typeinfos);
            return decoder.instance(game_details_typeid) as PyDictionary;
        }
        public PyEnumerableObject decode_replay_initdata(byte[] contents)
        {
            var decoder = new BitPackedDecoder(contents, typeinfos);
            return decoder.instance(replay_initdata_typeid) as PyEnumerableObject;
        }
        public PyEnumerableObject decode_replay_attributes_events(byte[] contents)
        {
            var buffer = new BitPackedBuffer(contents, "little");
            PyDictionary attributes = new PyDictionary();
            if (!buffer.done())
            {
                attributes["source"] = buffer.read_bits(8);
                attributes["mapNamespace"] = buffer.read_bits(32);
                var count = buffer.read_bits(32);
                attributes["scopes"] = new PyDictionary();
                while (!buffer.done())
                {
                    var value = new PyDictionary();
                    value["namespace"] = buffer.read_bits(32);
                    var attrid = buffer.read_bits(32);
                    value["attrid"] = attrid;
                    var scope = buffer.read_bits(8);
                    value["value"] = buffer.read_aligned_bytes(4).Reverse().ToArray();
                    if (!((PyDictionary)attributes["scopes"]).ContainsKey(scope))
                        ((PyDictionary)attributes["scopes"])[scope] = new PyDictionary();
                    if (!((PyDictionary)((PyDictionary)attributes["scopes"])[scope]).ContainsKey(attrid))
                        ((PyDictionary)((PyDictionary)attributes["scopes"])[scope])[attrid] = new PyList();
                    ((PyList)((PyDictionary)((PyDictionary)attributes["scopes"])[scope])[attrid]).Add(value);
                }
            }
            return attributes;
        }

        public List<(long bitOffset, long gameloop)> _skip_event_stream(BitPackedDecoder decoder, int eventid_typeid, PyDictionary event_types, bool decode_user_id)
        {
            long gameloop = 0;
            List<(long bitOffset, long gameloop)> result = new List<(long bitOffset, long gameloop)>();
            while (!decoder.done())
            {
                var start_bits = decoder.used_bits();
                var delta = _varuint32_value((PyDictionary)decoder.instance(svaruint32_typeid));
                gameloop += delta;
                if (decode_user_id)
                {
                    decoder.skip_instance(replay_userid_typeid);
                }
                var eventid = decoder.instance(eventid_typeid);
                var typeid = (event_types[eventid] as PyEnumerableObject)?[0];
                if (typeid is null)
                    throw new Exception($"eventid({eventid}) at {decoder}");
                decoder.skip_instance((dynamic)typeid);
                decoder.byte_align();
                result.Add((start_bits, gameloop));
            }
            return result;
        }
        public List<(long bitOffset, long gameloop)> skip_replay_game_events(byte[] contents)
        {
            return _skip_event_stream(
                new BitPackedDecoder(contents, typeinfos),
                game_eventid_typeid,
                game_event_types,
                true);
        }

        /// <summary>
        /// 利用多线程来加速事件解析,比decode_replay_game_events快3到4倍
        /// 解析回放文件中的replay.game.events
        /// 注意:该过程可能会使用大量内存,并且GC回收内存时机是不确定的
        /// 当你不使用返回值后,程序任然占用了大量的内存这是正常的,并非内存泄露 因为GC还没有回收内存
        /// 因此 在你使用完返回值后 你可能需要主动调用GC.Collect();来回收内存
        /// </summary>
        /// <param name="contents">replay.game.events文件的字节数据</param>
        /// <returns></returns>
        public List<PyDictionary> quick_decode_replay_game_events(byte[] contents)
        {
            var bitOffsetGameloops = skip_replay_game_events(contents).ToArray();
            int threadCount = Environment.ProcessorCount;
            List<PyDictionary>[] results = new List<PyDictionary>[threadCount];
            Parallel.For(0, threadCount, i =>
            {
                int start = i * (bitOffsetGameloops.Length / threadCount);
                int end = (i == threadCount - 1) ? bitOffsetGameloops.Length : (i + 1) * (bitOffsetGameloops.Length / threadCount);
                List<PyDictionary> threadResult = new List<PyDictionary>();
                var decoder = new BitPackedDecoder(contents, typeinfos);
                for (int j = start; j < end; j++)
                {
                    decoder.seek(bitOffsetGameloops[j].bitOffset);
                    threadResult.Add(decode_event_stream_work(decoder, bitOffsetGameloops[j].gameloop));
                }
                results[i] = threadResult;
            });
            return results.SelectMany(r => r).ToList();
        }

        private PyDictionary decode_event_stream_work(BitPackedDecoder decoder,long gameloop)
        {
            var start_bits = decoder.used_bits();
            decoder.skip_instance(svaruint32_typeid);
            //var delta = _varuint32_value((PyDictionary)decoder.instance(svaruint32_typeid));
            //gameloop += delta;
            object userid = null;
            userid = decoder.instance(replay_userid_typeid);
            var eventid = decoder.instance(game_eventid_typeid);

            var typeid = (game_event_types[eventid] as PyEnumerableObject)?[0];
            var typename = (game_event_types[eventid] as PyEnumerableObject)?[1];
            if (typeid is null)
                throw new Exception($"eventid({eventid}) at {decoder}");
            PyDictionary @event = decoder.instance((dynamic)typeid);
            @event["_event"] = typename;
            @event["_eventid"] = eventid;
            @event["_gameloop"] = gameloop;
            @event["_userid"] = userid;
            decoder.byte_align();
            @event["_bits"] = decoder.used_bits() - start_bits;

            return @event;
        }


    }
    public class Replay
    {
        public struct Color
        {
            public byte m_a;
            public byte m_r;
            public byte m_g;
            public byte m_b;

            public Color(PyDictionary color)
            {
                m_a = (byte)(long)(color["m_a"]);
                m_r = (byte)(long)(color["m_r"]);
                m_g = (byte)(long)(color["m_g"]);
                m_b = (byte)(long)(color["m_b"]);
            }

            public override string ToString()
            {
                return $"ARGB:{m_a:X}{m_r:X}{m_g:X}{m_b:X}";
            }
        }
        public struct Handle
        {
            public long m_region;
            public string m_programId;
            public long m_realm;
            public long m_id;

            public Handle(PyDictionary pyDictionary)
            {
                m_region = (dynamic)pyDictionary["m_region"];
                m_programId = Encoding.UTF8.GetString((dynamic)pyDictionary["m_programId"]).Replace("\0", "");
                m_realm = (dynamic)pyDictionary["m_realm"];
                m_id = (dynamic)pyDictionary["m_id"];
            }
            public Handle(String handle){
                var s = handle.Split('-');
                m_region = long.Parse(s[0]);
                m_programId = s[1];
                m_realm = long.Parse(s[2]);
                m_id = long.Parse(s[3]);
            }
            public override string ToString()
            {
                return $"{m_region}-{m_programId}-{m_realm}-{m_id}";
            }
        }
        public struct Player
        {
            public Color m_color;
            public long m_control;
            public long m_handicap;
            public string m_hero;
            public string m_name;
            public long m_observe;
            public string m_race;
            public long m_result;
            public long m_teamId;
            public Handle m_toon;
            public long m_workingSetSlotId;

            public Player(PyDictionary player)
            {
                m_color = new Color((PyDictionary)player["m_color"]);
                m_toon = new Handle((PyDictionary)player["m_toon"]);
                m_control = (dynamic)player["m_control"];
                m_handicap = (dynamic)player["m_handicap"];
                m_hero = Encoding.UTF8.GetString((dynamic)player["m_hero"]);
                m_name = Encoding.UTF8.GetString((dynamic)player["m_name"]);
                m_observe = (dynamic)player["m_observe"];
                m_race = Encoding.UTF8.GetString((dynamic)player["m_race"]);
                m_result = (dynamic)player["m_result"];
                m_teamId = (dynamic)player["m_teamId"];
                m_workingSetSlotId = (dynamic)player["m_workingSetSlotId"];
            }
            public override string ToString()
            {
                return $"{m_toon} {m_name}";
            }
        }

        public struct Header
        {
            public struct Version
            {
                public long m_flags;
                public long m_major;
                public long m_minor;
                public long m_revision;
                public long m_build;
                public long m_baseBuild;

                public Version(PyDictionary ersion)
                {
                    m_flags = (dynamic)ersion["m_flags"];
                    m_major = (dynamic)ersion["m_major"];
                    m_minor = (dynamic)ersion["m_minor"];
                    m_revision = (dynamic)ersion["m_revision"];
                    m_build = (dynamic)ersion["m_build"];
                    m_baseBuild = (dynamic)ersion["m_baseBuild"];
                }
            }
            public byte[] m_signature;
            public Version m_version;
            public long m_type;
            public long m_elapsedGameLoops;
            public bool m_useScaledTime;
            public PyDictionary m_ngdpRootKey;
            public long m_dataBuildNum;
            public PyDictionary m_replayCompatibilityHash;
            public bool m_ngdpRootKeyIsDevData;
            public Header(PyDictionary Structure)
            {
                m_signature = (dynamic)Structure["m_signature"];
                m_version = new Version((dynamic)Structure["m_version"]);
                m_type = (dynamic)Structure["m_type"];
                m_elapsedGameLoops = (dynamic)Structure["m_elapsedGameLoops"];
                m_useScaledTime = (dynamic)Structure["m_useScaledTime"];
                m_ngdpRootKey = (dynamic)Structure["m_ngdpRootKey"];
                m_dataBuildNum = (dynamic)Structure["m_dataBuildNum"];
                m_replayCompatibilityHash = (dynamic)Structure["m_replayCompatibilityHash"];
                m_ngdpRootKeyIsDevData = (dynamic)Structure["m_ngdpRootKeyIsDevData"];
            }
        }
        public struct Details
        {
            public string m_imageFilePath;
            public bool m_isBlizzardMap;
            public string m_mapFileName;
            public bool m_miniSave;
            public object m_modPaths;
            public bool m_restartAsTransitionMap;
            public long m_timeLocalOffset;
            public long m_timeUTC;
            public string m_title;
            public Dictionary<string, string> m_thumbnail;
            public List<byte[]> m_cacheHandles;
            public List<Player> m_playerList;

            public Details(PyDictionary Structure)
            {
                m_thumbnail = new Dictionary<string, string>();
                m_cacheHandles = new List<byte[]>();
                m_playerList = new List<Player>();

                m_imageFilePath = Encoding.UTF8.GetString((dynamic)Structure["m_imageFilePath"]);
                m_isBlizzardMap = (dynamic)Structure["m_isBlizzardMap"];
                m_mapFileName = Encoding.UTF8.GetString((dynamic)Structure["m_mapFileName"]);
                m_miniSave = (dynamic)Structure["m_miniSave"];
                m_modPaths = (dynamic)Structure["m_modPaths"];
                m_restartAsTransitionMap = (dynamic)Structure["m_restartAsTransitionMap"];
                m_timeLocalOffset = (dynamic)Structure["m_timeLocalOffset"];
                m_timeUTC = (dynamic)Structure["m_timeUTC"];
                m_title = Encoding.UTF8.GetString((dynamic)Structure["m_title"]);
                foreach (var e in (PyDictionary)Structure["m_thumbnail"])
                {
                    m_thumbnail[(dynamic)e] = Encoding.UTF8.GetString((dynamic)((PyDictionary)Structure["m_thumbnail"])[e]);
                }
                foreach (var e in (PyList)Structure["m_cacheHandles"])
                {
                    m_cacheHandles.Add((dynamic)e);
                }
                foreach (PyDictionary player in (PyList)Structure["m_playerList"])
                {
                    m_playerList.Add(new Player(player));
                }

            }
        }
        public abstract class Message
        {
            public int EventId;
            public int GameLoop;
            public int UserId;

            protected Message(PyDictionary message)
            {
                EventId = (int)((long)message["_eventid"]);
                GameLoop = (int)((long)message["_gameloop"]);
                UserId = (int)((long)((PyDictionary)message["_userid"])["m_userId"]);
            }
            public override string ToString()
            {
                return $"[{GetType().Name}] EventId:{EventId} GameLoop:{GameLoop} UserId:{UserId}";
            }
            public static List<Message> Parse(List<PyDictionary> MessageEvents)
            {
                List<Message> result = new List<Message>();
                foreach (var e in MessageEvents)
                {
                    switch (e["_event"])
                    {
                        case "NNet.Game.SChatMessage":
                            result.Add(new ChatMessage(e));
                            break;
                        case "NNet.Game.SPingMessage":
                            result.Add(new PingMessage(e));
                            break;
                        case "NNet.Game.SLoadingProgressMessage":
                            result.Add(new LoadingProgressMessage(e));
                            break;
                        case "NNet.Game.SServerPingMessage":
                            result.Add(new ServerPingMessage(e));
                            break;
                        case "NNet.Game.SReconnectNotifyMessage":
                            result.Add(new ReconnectNotifyMessage(e));
                            break;
                    }
                }
                return result;
            }

            public class ChatMessage : Message
            {
                public string Content;
                public ChatMessage(PyDictionary message) : base(message)
                {
                    Content = Encoding.UTF8.GetString((dynamic)message["m_string"]);
                }
                public override string ToString()
                {
                    return $"{base.ToString()} Message:{Content}";
                }
            }
            public class PingMessage : Message
            {
                public PingMessage(PyDictionary message) : base(message)
                {

                }
            }
            public class LoadingProgressMessage : Message
            {
                public int Progress;

                public LoadingProgressMessage(PyDictionary message) : base(message)
                {
                    Progress = (int)((long)message["m_progress"]);

                }
                public override string ToString()
                {
                    return $"{base.ToString()} Progress:{Progress}";
                }
            }
            public class ServerPingMessage : Message
            {
                public ServerPingMessage(PyDictionary message) : base(message)
                {
                }
            }
            public class ReconnectNotifyMessage : Message
            {
                public ReconnectNotifyMessage(PyDictionary message) : base(message)
                {
                }
            }
        }
        public abstract class Event
        {
            public int EventId;
            public int GameLoop;
            public int UserId;
            protected Event(PyDictionary message)
            {
                EventId = (int)((long)message["_eventid"]);
                GameLoop = (int)((long)message["_gameloop"]);
                UserId = (int)((long)((PyDictionary)message["_userid"])["m_userId"]);
            }
            public abstract class BankEvent : Event
            {
                protected BankEvent(PyDictionary message) : base(message){}
            }
            public class BankFileEvent : BankEvent
            {
                string FileName;
                public BankFileEvent(PyDictionary @event) : base(@event)
                {
                    FileName = Encoding.UTF8.GetString((dynamic)@event["m_name"]);
                }
                public override string ToString()
                {
                    return $"[{GetType().Name}] FileName:{FileName}";
                }
            }
            public class BankSectionEvent : BankEvent
            {
                string SectionName;
                public BankSectionEvent(PyDictionary @event) : base(@event)
                {
                    SectionName = Encoding.UTF8.GetString((dynamic)@event["m_name"]);
                }
                public override string ToString()
                {
                    return $"[{GetType().Name}] SectionName:{SectionName}";
                }
            }
            public class BankKeyEvent : BankEvent
            {
                string KeyName;
                KeyType type;
                string data;
                public enum KeyType
                {
                    Fixed = 0,
                    Flag = 1,
                    Int = 2,
                    String = 3,
                    Unit = 4,
                    Point = 5,
                    Text = 6,
                    Complex=7,
                }
                public BankKeyEvent(PyDictionary @event) : base(@event)
                {
                    KeyName = Encoding.UTF8.GetString((dynamic)@event["m_name"]);
                    type = (KeyType)(long)@event["m_type"];
                    data = Encoding.UTF8.GetString((dynamic)@event["m_data"]);
                }
                public override string ToString()
                {
                    return $"[{GetType().Name}] {KeyName} {type}={data}";
                }
            }
            public class BankValueEvent : BankEvent
            {
                string Name;
                ValueType type;
                object Value;
                public enum ValueType
                {
                    Fixed = 0,
                    Flag = 1,
                    Int = 2,
                    String = 3,
                    Unit = 4,
                    Point = 5,
                    Text = 6,
                }
                public BankValueEvent(PyDictionary @event) : base(@event)
                {
                    Name = Encoding.UTF8.GetString((dynamic)@event["m_name"]);
                    type = (ValueType)(long)@event["m_type"];
                    
                    string sValue= Encoding.UTF8.GetString((dynamic)@event["m_data"]);
                    switch (type)
                    {
                        case ValueType.Fixed: Value =float.Parse(sValue) ; break;
                        case ValueType.Flag: Value = sValue == "1";  break;
                        case ValueType.Int: Value = Value = int.Parse(sValue); break;
                        case ValueType.String: Value = sValue; break;
                        case ValueType.Unit: Value = sValue; break;
                        case ValueType.Point: Value = sValue; break;
                        case ValueType.Text: Value = sValue; break;
                        default: Value = @event["m_data"]; break;
                    }


                }
                public override string ToString()
                {
                    return $"[{GetType().Name}] {Name} { type}={Value}";
                }
            }
            public class BankSignatureEvent : BankEvent
            {
                byte[] Signature;
                Handle Handle;
                public BankSignatureEvent(PyDictionary @event) : base(@event)
                {
                    Signature = ((PyList)@event["m_signature"]).Select(n => (byte)((long)n)).ToArray();
                    Handle = new Handle(Encoding.UTF8.GetString((dynamic)@event["m_toonHandle"]));
                }
                public override string ToString()
                {
                    return $"[{GetType().Name}] {Handle} Signature:{BitConverter.ToString(Signature).Replace("-","")}";
                }
            }


            public static List<Event> Parse(List<PyDictionary>GameEvents)
            {
                List<Event> result = new List<Event>();
                foreach (var e in GameEvents)
                {
                    switch (e["_event"])
                    {
                        case "NNet.Game.SUserFinishedLoadingSyncEvent": break;
                        case "NNet.Game.SUserOptionsEvent": break;
                        case "NNet.Game.SBankFileEvent": result.Add(new BankFileEvent(e)); break;
                        case "NNet.Game.SBankSectionEvent": result.Add(new BankSectionEvent(e)); break;
                        case "NNet.Game.SBankKeyEvent": result.Add(new BankKeyEvent(e)); break;
                        case "NNet.Game.SBankValueEvent": result.Add(new BankValueEvent(e)); break;
                        case "NNet.Game.SBankSignatureEvent": result.Add(new BankSignatureEvent(e)); break;
                        case "NNet.Game.SCameraSaveEvent": break;
                        case "NNet.Game.SSaveGameEvent": break;
                        case "NNet.Game.SSaveGameDoneEvent": break;
                        case "NNet.Game.SLoadGameDoneEvent": break;
                        case "NNet.Game.SCommandManagerResetEvent": break;
                        case "NNet.Game.SGameCheatEvent": break;
                        case "NNet.Game.SCmdEvent": break;
                        case "NNet.Game.SSelectionDeltaEvent": break;
                        case "NNet.Game.SControlGroupUpdateEvent": break;
                        case "NNet.Game.SSelectionSyncCheckEvent": break;
                        case "NNet.Game.SResourceTradeEvent": break;
                        case "NNet.Game.STriggerChatMessageEvent": break;
                        case "NNet.Game.SAICommunicateEvent": break;
                        case "NNet.Game.SSetAbsoluteGameSpeedEvent": break;
                        case "NNet.Game.SAddAbsoluteGameSpeedEvent": break;
                        case "NNet.Game.STriggerPingEvent": break;
                        case "NNet.Game.SBroadcastCheatEvent": break;
                        case "NNet.Game.SAllianceEvent": break;
                        case "NNet.Game.SUnitClickEvent": break;
                        case "NNet.Game.SUnitHighlightEvent": break;
                        case "NNet.Game.STriggerReplySelectedEvent": break;
                        case "NNet.Game.SHijackReplayGameEvent": break;
                        case "NNet.Game.STriggerSkippedEvent": break;
                        case "NNet.Game.STriggerSoundLengthQueryEvent": break;
                        case "NNet.Game.STriggerSoundOffsetEvent": break;
                        case "NNet.Game.STriggerTransmissionOffsetEvent": break;
                        case "NNet.Game.STriggerTransmissionCompleteEvent": break;
                        case "NNet.Game.SCameraUpdateEvent": break;
                        case "NNet.Game.STriggerAbortMissionEvent": break;
                        case "NNet.Game.STriggerPurchaseMadeEvent": break;
                        case "NNet.Game.STriggerPurchaseExitEvent": break;
                        case "NNet.Game.STriggerPlanetMissionLaunchedEvent": break;
                        case "NNet.Game.STriggerPlanetPanelCanceledEvent": break;
                        case "NNet.Game.STriggerDialogControlEvent": break;
                        case "NNet.Game.STriggerSoundLengthSyncEvent": break;
                        case "NNet.Game.STriggerConversationSkippedEvent": break;
                        case "NNet.Game.STriggerMouseClickedEvent": break;
                        case "NNet.Game.STriggerMouseMovedEvent": break;
                        case "NNet.Game.SAchievementAwardedEvent": break;
                        case "NNet.Game.STriggerHotkeyPressedEvent": break;
                        case "NNet.Game.STriggerTargetModeUpdateEvent": break;
                        case "NNet.Game.STriggerPlanetPanelReplayEvent": break;
                        case "NNet.Game.STriggerSoundtrackDoneEvent": break;
                        case "NNet.Game.STriggerPlanetMissionSelectedEvent": break;
                        case "NNet.Game.STriggerKeyPressedEvent": break;
                        case "NNet.Game.STriggerMovieFunctionEvent": break;
                        case "NNet.Game.STriggerPlanetPanelBirthCompleteEvent": break;
                        case "NNet.Game.STriggerPlanetPanelDeathCompleteEvent": break;
                        case "NNet.Game.SResourceRequestEvent": break;
                        case "NNet.Game.SResourceRequestFulfillEvent": break;
                        case "NNet.Game.SResourceRequestCancelEvent": break;
                        case "NNet.Game.STriggerResearchPanelExitEvent": break;
                        case "NNet.Game.STriggerResearchPanelPurchaseEvent": break;
                        case "NNet.Game.STriggerResearchPanelSelectionChangedEvent": break;
                        case "NNet.Game.STriggerCommandErrorEvent": break;
                        case "NNet.Game.STriggerMercenaryPanelExitEvent": break;
                        case "NNet.Game.STriggerMercenaryPanelPurchaseEvent": break;
                        case "NNet.Game.STriggerMercenaryPanelSelectionChangedEvent": break;
                        case "NNet.Game.STriggerVictoryPanelExitEvent": break;
                        case "NNet.Game.STriggerBattleReportPanelExitEvent": break;
                        case "NNet.Game.STriggerBattleReportPanelPlayMissionEvent": break;
                        case "NNet.Game.STriggerBattleReportPanelPlaySceneEvent": break;
                        case "NNet.Game.STriggerBattleReportPanelSelectionChangedEvent": break;
                        case "NNet.Game.STriggerVictoryPanelPlayMissionAgainEvent": break;
                        case "NNet.Game.STriggerMovieStartedEvent": break;
                        case "NNet.Game.STriggerMovieFinishedEvent": break;
                        case "NNet.Game.SDecrementGameTimeRemainingEvent": break;
                        case "NNet.Game.STriggerPortraitLoadedEvent": break;
                        case "NNet.Game.STriggerCustomDialogDismissedEvent": break;
                        case "NNet.Game.STriggerGameMenuItemSelectedEvent": break;
                        case "NNet.Game.STriggerMouseWheelEvent": break;
                        case "NNet.Game.STriggerPurchasePanelSelectedPurchaseItemChangedEvent": break;
                        case "NNet.Game.STriggerPurchasePanelSelectedPurchaseCategoryChangedEvent": break;
                        case "NNet.Game.STriggerButtonPressedEvent": break;
                        case "NNet.Game.STriggerGameCreditsFinishedEvent": break;
                        case "NNet.Game.STriggerCutsceneBookmarkFiredEvent": break;
                        case "NNet.Game.STriggerCutsceneEndSceneFiredEvent": break;
                        case "NNet.Game.STriggerCutsceneConversationLineEvent": break;
                        case "NNet.Game.STriggerCutsceneConversationLineMissingEvent": break;
                        case "NNet.Game.SGameUserLeaveEvent": break;
                        case "NNet.Game.SGameUserJoinEvent": break;
                        case "NNet.Game.SCommandManagerStateEvent": break;
                        case "NNet.Game.SCmdUpdateTargetPointEvent": break;
                        case "NNet.Game.SCmdUpdateTargetUnitEvent": break;
                        case "NNet.Game.STriggerAnimLengthQueryByNameEvent": break;
                        case "NNet.Game.STriggerAnimLengthQueryByPropsEvent": break;
                        case "NNet.Game.STriggerAnimOffsetEvent": break;
                        case "NNet.Game.SCatalogModifyEvent": break;
                        case "NNet.Game.SHeroTalentTreeSelectedEvent": break;
                        case "NNet.Game.STriggerProfilerLoggingFinishedEvent": break;
                        case "NNet.Game.SHeroTalentTreeSelectionPanelToggledEvent": break;
                        case "NNet.Game.SSetSyncLoadingTimeEvent": break;
                        case "NNet.Game.SSetSyncPlayingTimeEvent": break;
                        case "NNet.Game.SPeerSetSyncLoadingTimeEvent": break;
                        case "NNet.Game.SPeerSetSyncPlayingTimeEvent": break;
                    }
                }
                return result;
            }
        }
    }
    public class Utils
    {
        public static string FindFirstAssignment(string pythonCodeFilePath, string variableName)
        {
            try
            {
                string[] lines = File.ReadAllLines(pythonCodeFilePath);
                bool variableFound = false;
                StringBuilder fullAssignment = new StringBuilder(); 
                Stack<char> structureStack = new Stack<char>();
                string pattern = $@"\b{variableName}\b\s*=";

                foreach (var line in lines)
                {
                    if (!variableFound)
                    {
                        if (Regex.IsMatch(line, pattern))
                        {
                            variableFound = true;
                            fullAssignment.AppendLine(line.Substring(line.IndexOf("=") + 1).Trim());
                            foreach (char ch in fullAssignment.ToString())
                            {
                                if (ch == '{' || ch == '[' || ch == '(')
                                    structureStack.Push(ch);
                            }
                        }
                    }
                    else
                    {
                        fullAssignment.AppendLine(line.Trim());
                        foreach (char ch in line)
                        {
                            if (ch == '{' || ch == '[' || ch == '(')
                                structureStack.Push(ch);
                            else if ((ch == '}' && structureStack.Peek() == '{') ||
                                     (ch == ']' && structureStack.Peek() == '[') ||
                                     (ch == ')' && structureStack.Peek() == '('))
                            {
                                structureStack.Pop();
                            }
                        }

                        if (structureStack.Count == 0)
                            break; 
                    }
                }
                if (variableFound)
                {
                    string result = fullAssignment.ToString().Trim();
                    if (result == "None") return null;
                    return result;
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
        /// <summary>
        /// 通过遍历s2protocol的versions文件夹生成C#版本的protocol代码
        /// </summary>
        /// <param name="path">s2protocol的versions文件夹路径</param>
        public static void ExportPythonProtocol(string path)
        {
            //string[] files = Directory.GetFiles(@"D:\Desktop\s2protocol.NET-master\src\s2protocol.NET\libs2\s2protocol\versions", "protocol*.py", SearchOption.TopDirectoryOnly);
            string[] files = Directory.GetFiles(path, "protocol*.py", SearchOption.TopDirectoryOnly);
            File.Delete("versions.cs");
            foreach (string file in files)
            {
                string version = Path.GetFileName(file).Split(new[] { "protocol", ".py" }, StringSplitOptions.RemoveEmptyEntries)[0];
                string typeinfos = FindFirstAssignment(file, "typeinfos");
                string game_event_types = FindFirstAssignment(file, "game_event_types");
                string message_event_types = FindFirstAssignment(file, "message_event_types");
                string tracker_event_types = FindFirstAssignment(file, "tracker_event_types");
                string game_eventid_typeid = FindFirstAssignment(file, "game_eventid_typeid"); ;
                string message_eventid_typeid = FindFirstAssignment(file, "message_eventid_typeid"); ;
                string tracker_eventid_typeid = FindFirstAssignment(file, "tracker_eventid_typeid"); ;
                string svaruint32_typeid = FindFirstAssignment(file, "svaruint32_typeid"); ;
                string replay_userid_typeid = FindFirstAssignment(file, "replay_userid_typeid"); ;
                string replay_header_typeid = FindFirstAssignment(file, "replay_header_typeid"); ;
                string game_details_typeid = FindFirstAssignment(file, "game_details_typeid"); ;
                string replay_initdata_typeid = FindFirstAssignment(file, "replay_initdata_typeid"); ;
                string code = $@"
public class Protocol{version} : S2Protocol{{
    public Protocol{version}(){{
";
                if (typeinfos != null) code += $@"        typeinfos = new Parser(@""{typeinfos}"").Parse() as PyList;
";
                if (game_event_types != null) code += $@"        game_event_types = new Parser(@""{game_event_types}"").Parse() as PyDictionary;
";
                if (message_event_types != null) code += $@"        message_event_types = new Parser(@""{message_event_types}"").Parse() as PyDictionary;
";
                if (tracker_event_types != null) code += $@"        tracker_event_types = new Parser(@""{tracker_event_types}"").Parse() as PyDictionary;
";
                if (game_eventid_typeid != null) code += $@"        game_eventid_typeid = {game_eventid_typeid};
";
                if (message_eventid_typeid != null) code += $@"        message_eventid_typeid = {message_eventid_typeid};
";
                if (tracker_eventid_typeid != null) code += $@"        tracker_eventid_typeid = {tracker_eventid_typeid};
";
                if (svaruint32_typeid != null) code += $@"        svaruint32_typeid = {svaruint32_typeid};
";
                if (replay_userid_typeid != null) code += $@"        replay_userid_typeid = {replay_userid_typeid};
";
                if (replay_header_typeid != null) code += $@"        replay_header_typeid = {replay_header_typeid};
";
                if (game_details_typeid != null) code += $@"        game_details_typeid = {game_details_typeid};
";
                if (replay_initdata_typeid != null) code += $@"        replay_initdata_typeid = {replay_initdata_typeid};
";
                code += $@"
        if(!Build.ContainsKey({version})) Build.Add({version}, this);
    }}
}}";

                File.AppendAllText("versions.cs", code);
            }
        }
    }
}
