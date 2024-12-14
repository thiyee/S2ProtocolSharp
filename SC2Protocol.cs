using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using System.Linq;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;

namespace SC2Protocol
{
    public abstract class PyEnumerableObject : IEnumerable<object>
    {
        // 实现 GetEnumerator 方法
        public abstract IEnumerator<object> GetEnumerator();

        // 这通常是为了支持 foreach 或其他迭代操作，返回 IEnumerator
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
                // 如果 index 是 BigInteger，转换为 int
                if (index is BigInteger bigIndex)
                {
                    // 确保 BigInteger 在 int 范围内
                    if (bigIndex >= int.MinValue && bigIndex <= int.MaxValue)
                    {
                        return objects[(int)bigIndex];
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("index", "BigInteger value is out of range for an int.");
                    }
                }

                // 如果 index 已经是 int 类型
                if (index is int intIndex)
                {
                    return objects[intIndex];
                }

                throw new ArgumentException("Index must be either int or BigInteger.");
            }
            set
            {
                // 如果 index 是 BigInteger，转换为 int
                if (index is BigInteger bigIndex)
                {
                    // 确保 BigInteger 在 int 范围内
                    if (bigIndex >= int.MinValue && bigIndex <= int.MaxValue)
                    {
                        objects[(int)bigIndex] = value;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("index", "BigInteger value is out of range for an int.");
                    }
                }
                else if (index is int intIndex)
                {
                    objects[intIndex] = value;
                }
                else
                {
                    throw new ArgumentException("Index must be either int or BigInteger.");
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
    public class Parser
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
                    var Value = BigInteger.Parse(Literal);
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
            object key = null;  // Store the key for the dictionary
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
        private int _used;
        private int? _next;
        private int _nextBits;
        private bool _bigEndian;

        public BitPackedBuffer(byte[] contents, string endian = "big")
        {
            _data = contents ?? Array.Empty<byte>();
            _used = 0;
            _next = null;
            _nextBits = 0;
            _bigEndian = (endian == "big");
        }

        public override string ToString()
        {
            string s = (_used < _data.Length) ? _data[_used].ToString("X2") : "--";
            return $"buffer({(_nextBits != 0 ? (_next ?? 0) : 0):X2}/{_nextBits},[{_used}]={s})";
        }

        public bool done()
        {
            return _nextBits == 0 && _used >= _data.Length;
        }

        public int used_bits()
        {
            return _used * 8 - _nextBits;
        }

        public void byte_align()
        {
            _nextBits = 0;
        }

        public byte[] read_aligned_bytes(BigInteger bytes)
        {
            if (bytes > int.MaxValue) throw new InvalidOperationException("Length OutofRange.");
            int lbytes = (int)bytes;
            byte_align();
            if (_used + bytes > _data.Length)
            {
                throw new InvalidOperationException("Buffer is truncated.");
            }
            var result = _data.Skip(_used).Take(lbytes).ToArray();
            _used += lbytes;
            return result;
        }
        public BigInteger read_bits(BigInteger bits)
        {
            if (bits > int.MaxValue) throw new InvalidOperationException("Length OutofRange.");
            int lbits = (int)bits;
            BigInteger result = 0; // 使用 BigInteger 类型
            int resultBits = 0;

            while (resultBits != lbits)
            {
                if (_nextBits == 0)
                {
                    if (done())
                    {
                        throw new InvalidOperationException("Buffer is truncated.");
                    }
                    _next = _data[_used++];
                    _nextBits = 8;
                }

                int copyBits = Math.Min(lbits - resultBits, _nextBits);
                int copy = (_next.Value & ((1 << copyBits) - 1));

                if (_bigEndian)
                {
                    result |= (BigInteger)copy << (lbits - resultBits - copyBits); // 使用 BigInteger 类型
                }
                else
                {
                    result |= (BigInteger)copy << resultBits; // 使用 BigInteger 类型
                }

                _next >>= copyBits;
                _nextBits -= copyBits;
                resultBits += copyBits;
            }

            return result;
        }
        public byte[] read_unaligned_bytes(BigInteger bytes)
        {
            if (bytes > int.MaxValue) throw new InvalidOperationException("Length OutofRange.");
            int lbytes = (int)bytes;

            var result = new byte[lbytes];
            for (int i = 0; i < lbytes; i++)
            {
                result[i] = (byte)read_bits(8);
            }
            return result;
        }
    }
    public class BitPackedDecoder
    {
        private BitPackedBuffer _buffer;
        private PyList _typeinfos;
        private BitPackedDecoder self => this;
        public BitPackedDecoder(byte[] contents, PyList typeInfos)
        {
            _buffer = new BitPackedBuffer(contents);
            _typeinfos = typeInfos;
        }
        public override string ToString()
        {
            return _buffer.ToString();
        }
        public object instance(BigInteger typeId)
        {
            if (typeId >= _typeinfos.Count)
            {
                throw new InvalidOperationException("Invalid type ID.");
            }
            var typeInfo = (PyEnumerableObject)_typeinfos[typeId];
            var Method = GetType().GetMethod((string)typeInfo[0]);
            return Method.Invoke(this, ((PyEnumerableObject)typeInfo[1]).ToArray());
        }
        public object instance(int typeId) => instance((BigInteger)typeId);
        public void byte_align()
        {
            _buffer.byte_align();
        }
        public bool done()
        {
            return _buffer.done();
        }
        public int used_bits()
        {
            return _buffer.used_bits();
        }

        public PyList _array(PyTuple bounds, BigInteger typeid)
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
            return self._int(new PyTuple(0, 1)) != 0;
        }

        public PyDictionary _choice(PyTuple bounds, PyDictionary fields)
        {
            var tag = self._int(bounds);
            if (fields.ContainsKey(tag))
                throw new Exception("CorruptedError");
            var field = (PyEnumerableObject)fields[tag];
            return new PyDictionary() { { field[0], self.instance((BigInteger)field[1]) } };
        }
        public byte[] _fourcc()
        {
            return self._buffer.read_unaligned_bytes(4);
        }
        public BigInteger _int(PyTuple bounds)
        {
            return (BigInteger)bounds[0] + self._buffer.read_bits((int)bounds[1]);
        }
        public object _null()
        {
            return null;
        }
        public object _optional(BigInteger typeid)
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
        public PyDictionary _struct(PyDictionary fields)
        {
            PyDictionary result = new PyDictionary();

            foreach (PyEnumerableObject field in fields)
            {
                if (field[0] is string s && s == "__parent")
                {
                    var parent = self.instance((BigInteger)field[1]);
                    if (parent is PyDictionary parentDict)
                    {
                        foreach (var key in parentDict)
                        {
                            result[key] = parentDict[key];
                        }
                    }
                    else if (fields.Count == 1)
                        return parent as PyDictionary ?? new PyDictionary();
                    else
                        result[field[0]] = parent;
                }
                else
                    result[field[0]] = self.instance((BigInteger)field[1]);

            }

            return result;
        }


    }
    public class VersionedDecoder
    {
        private BitPackedBuffer _buffer;
        private PyList _typeinfos;
        private VersionedDecoder self => this;
        public VersionedDecoder(byte[] contents, PyList typeinfos)
        {
            self._buffer = new BitPackedBuffer(contents);
            self._typeinfos = typeinfos;
        }
        public override string ToString()
        {
            return _buffer.ToString();
        }
        public object instance(BigInteger typeId)
        {
            if (typeId >= _typeinfos.Count)
            {
                throw new InvalidOperationException("Invalid type ID.");
            }
            var typeInfo = (PyEnumerableObject)_typeinfos[typeId];
            var Method = GetType().GetMethod((string)typeInfo[0]);
            return Method.Invoke(this, ((PyEnumerableObject)typeInfo[1]).ToArray());
        }
        public object instance(int typeId) => instance((BigInteger)typeId);

        public void byte_align()
        {
            self._buffer.byte_align();
        }
        public bool done()
        {
            return self._buffer.done();
        }
        public int used_bits()
        {
            return self._buffer.used_bits();
        }
        public void _expect_skip(BigInteger expected)
        {
            if (self._buffer.read_bits(8) != expected)
                throw new Exception("CorruptedError");
        }
        public BigInteger _vint()
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
        public PyList _array(PyTuple bounds, BigInteger typeid)
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
            return new PyDictionary() { { field[0], self.instance((BigInteger)field[1]) } };
        }
        public byte[] _fourcc()
        {
            self._expect_skip(7);
            return self._buffer.read_aligned_bytes(4);
        }
        public BigInteger _int(PyTuple bounds)
        {
            self._expect_skip(9);
            return self._vint();
        }
        public object _null()
        {
            return null;
        }
        public object _optional(BigInteger typeid)
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

        static S2Protocol()
        {
            Build = new Dictionary<int, S2Protocol>();

            // 获取当前程序集中的所有类型
            var types = Assembly.GetExecutingAssembly().GetTypes();

            // 筛选所有继承自 S2Protocol 的非抽象类
            var subclasses = types.Where(t => t.IsSubclassOf(typeof(S2Protocol)) && !t.IsAbstract);

            foreach (var subclass in subclasses)
            {
                // 使用反射创建子类实例
                var instance = Activator.CreateInstance(subclass) as S2Protocol;
            }
        }
        public BigInteger _varuint32_value(PyDictionary value)
        {
            foreach (var v in value.Values)
            {
                return (BigInteger)v ;
            }
            return 0;
        }
        public IEnumerable<PyDictionary> _decode_event_stream(BitPackedDecoder decoder, int eventid_typeid, PyDictionary event_types, bool decode_user_id)
        {
            BigInteger gameloop = 0; 
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
                //yield return @event;
            }
            return result;
        }
        public IEnumerable<PyDictionary> _decode_event_stream(VersionedDecoder decoder, int eventid_typeid, PyDictionary event_types, bool decode_user_id)
        {
            BigInteger gameloop = 0;
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
                //yield return @event;
            }
            return result;
        }
        public IEnumerable<PyDictionary> decode_replay_game_events(byte[] contents)
        {
            var decoder = new BitPackedDecoder(contents, typeinfos);
            return _decode_event_stream(decoder, game_eventid_typeid, game_event_types, true);
        }
        public IEnumerable<PyDictionary> decode_replay_message_events(byte[] contents)
        {
            var decoder = new BitPackedDecoder(contents, typeinfos);
            return _decode_event_stream(decoder, message_eventid_typeid, message_event_types, true);
        }
        public IEnumerable<PyDictionary> decode_replay_tracker_events(byte[] contents)
        {
            var decoder = new VersionedDecoder(contents, typeinfos);
            return _decode_event_stream(decoder, tracker_eventid_typeid, tracker_event_types, false);
        }
        public PyEnumerableObject decode_replay_header(byte[] contents)
        {
            var decoder = new VersionedDecoder(contents, typeinfos);
            return decoder.instance(replay_header_typeid) as PyEnumerableObject;
        }
        public PyEnumerableObject decode_replay_details(byte[] contents)
        {
            var decoder = new VersionedDecoder(contents, typeinfos);
            return decoder.instance(game_details_typeid) as PyEnumerableObject;
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
    }

    public class Replay
    {
        public struct Color
        {
            public byte m_a;
            public byte m_r;
            public byte m_g;
            public byte m_b;
            public override string ToString()
            {
                return $"ARGB:{m_a:X}{m_r:X}{m_g:X}{m_b:X}";
            }
        }
        public struct Handle
        {
            public BigInteger m_region;
            public string m_programId;
            public BigInteger m_realm;
            public BigInteger m_id;
            public override string ToString()
            {
                return $"{m_region}-{m_programId}-{m_realm}-{m_id}";
            }
        }
        public struct Player
        {
            public Color m_color;
            public BigInteger m_control;
            public BigInteger m_handicap;
            public string m_hero;
            public string m_name;
            public BigInteger m_observe;
            public string m_race;
            public BigInteger m_result;
            public BigInteger m_teamId;
            public Handle m_toon;
            public BigInteger m_workingSetSlotId;
        }
        public class Details
        {
            public string m_imageFilePath;
            public bool m_isBlizzardMap;
            public string m_mapFileName;
            public bool m_miniSave;
            public object m_modPaths;
            public bool m_restartAsTransitionMap;
            public BigInteger m_timeLocalOffset;
            public BigInteger m_timeUTC;
            public string m_title;
            public Dictionary<string, string> m_thumbnail = new Dictionary<string, string>();
            public List<byte[]> m_cacheHandles = new List<byte[]>();
            public List<Player> m_playerList = new List<Player>();

            public Details(PyDictionary Structure)
            {
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
                    Color color = new Color()
                    {
                        m_a = (byte)(BigInteger)(((PyDictionary)player["m_color"])["m_a"]),
                        m_r = (byte)(BigInteger)(((PyDictionary)player["m_color"])["m_r"]),
                        m_g = (byte)(BigInteger)(((PyDictionary)player["m_color"])["m_g"]),
                        m_b = (byte)(BigInteger)(((PyDictionary)player["m_color"])["m_b"]),
                    };
                    Handle handle = new Handle()
                    {
                        m_region = (dynamic)((PyDictionary)player["m_toon"])["m_region"],
                        m_programId = Encoding.UTF8.GetString((dynamic)((PyDictionary)player["m_toon"])["m_programId"]),
                        m_realm = (dynamic)((PyDictionary)player["m_toon"])["m_realm"],
                        m_id = (dynamic)((PyDictionary)player["m_toon"])["m_id"]
                    };
                    m_playerList.Add(
                        new Player()
                        {
                            m_color = color,
                            m_control = (dynamic)player["m_control"],
                            m_handicap = (dynamic)player["m_handicap"],
                            m_hero = Encoding.UTF8.GetString((dynamic)player["m_hero"]),
                            m_name = Encoding.UTF8.GetString((dynamic)player["m_name"]),
                            m_observe = (dynamic)player["m_observe"],
                            m_race = Encoding.UTF8.GetString((dynamic)player["m_race"]),
                            m_result = (dynamic)player["m_result"],
                            m_teamId = (dynamic)player["m_teamId"],
                            m_toon = handle,
                            m_workingSetSlotId = (dynamic)player["m_workingSetSlotId"],
                        }
                        );

                }

            }
        }
    }

    public class Utils
    {
        public static string FindFirstAssignment(string pythonCodeFilePath, string variableName)
        {
            try
            {
                // 读取文件内容
                string[] lines = File.ReadAllLines(pythonCodeFilePath);
                bool variableFound = false;
                StringBuilder fullAssignment = new StringBuilder(); // 使用 StringBuilder
                Stack<char> structureStack = new Stack<char>(); // 用栈来处理嵌套结构
                string pattern = $@"\b{variableName}\b\s*=";

                // 遍历每一行，查找首次赋值
                foreach (var line in lines)
                {
                    // 如果未找到赋值，则继续查找
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
                            break; // 结束捕获
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
