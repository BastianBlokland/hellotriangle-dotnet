using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using HT.Engine.Utils;

namespace HT.Engine.Parsing
{
    public readonly struct XmlAttribute : IEquatable<XmlAttribute>
    {
        public readonly string Name;
        public readonly string Value;

        public XmlAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }

        //Equality
        public static bool operator ==(XmlAttribute a, XmlAttribute b) => a.Equals(b);

        public static bool operator !=(XmlAttribute a, XmlAttribute b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is XmlAttribute && Equals((XmlAttribute)obj);

        public bool Equals(XmlAttribute other) => other.Name == Name && other.Value == Value;

        public override int GetHashCode() => Name.GetHashCode() ^ Value.GetHashCode();

        public override string ToString() => $"(Name: {Name}, Value: {Value})";
    }

    public readonly struct XmlTag : IEquatable<XmlTag>
    {
        public readonly string Name;
        public readonly IReadOnlyList<XmlAttribute> Attributes; //Null if no tags where set

        public XmlTag(string name, IReadOnlyList<XmlAttribute> attributes)
        {
            Name = name;
            Attributes = attributes;
        }

        public string GetAttributeValue(string attributeName)
        {
            if (Attributes == null)
                return null;
            for (int i = 0; i < Attributes.Count; i++)
            {
                if (Attributes[i].Name == attributeName)
                    return Attributes[i].Value;
            }
            return null;
        }

        //Equality
        public static bool operator ==(XmlTag a, XmlTag b) => a.Equals(b);

        public static bool operator !=(XmlTag a, XmlTag b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is XmlTag && Equals((XmlTag)obj);

        public bool Equals(XmlTag other)
        {
            if (Name != other.Name)
                return false;
            if (Attributes == null && other.Attributes == null)
                return true;
            if (Attributes == null || other.Attributes == null)
                return false;
            if (Attributes.Count != other.Attributes.Count)
                return false;
            for (int i = 0; i < Attributes.Count; i++)
                if (Attributes[i] != other.Attributes[i])
                    return false;
            return true;
        }

        public override int GetHashCode()
        {
            int hashcode = Name == null ? 1 : Name.GetHashCode();
            if (Attributes != null)
            {
                for (int i = 0; i < Attributes.Count; i++)
                    hashcode ^= Attributes[i].GetHashCode();
            }
            return hashcode;
        }

        public override string ToString()
            => $"(Name: {Name}, Attributes: {(Attributes == null ? 0 : Attributes.Count)})";
    }

    /// <summary>
    /// Pointers to start and ending offset positions in the file. Why not just store the raw string
    /// here to avoid having to do another pass over the file? Well some file formats put huge
    /// amounts of data in the elements (for example model data) and saving it all here in this
    /// structure would make this potentially take huge amounts of memory and with this approach
    /// you can keep working with this stream based approach where you can parse large files without
    /// needing to have the raw text all in memory at the same time.
    /// </summary>
    public readonly struct XmlDataEntry
    {
        //Properties
        public long ByteSize => EndBytePosition - StartBytePosition;

        //Data
        public readonly long StartBytePosition;
        public readonly long EndBytePosition;

        public XmlDataEntry(long startBytePosition, long endBytePosition)
        {
            if (endBytePosition < startBytePosition)
                throw new ArgumentOutOfRangeException(nameof(endBytePosition));
            StartBytePosition = startBytePosition;
            EndBytePosition = endBytePosition;
        }
    }

    public sealed class XmlElement
    {
        //Properties
        public IReadOnlyList<XmlDataEntry> Data => data;
        public IReadOnlyList<XmlElement> Children => children;
        public bool HasChildren => children != null && children.Count > 0;
        public XmlDataEntry? FirstData => (data == null || data.Count == 0) ? 
            null : (XmlDataEntry?)data[0];

        //Data
        public readonly XmlTag Tag;
        private readonly List<XmlDataEntry> data = new List<XmlDataEntry>();
        private readonly List<XmlElement> children = new List<XmlElement>();

        public XmlElement(XmlTag tag) => Tag = tag;

        public bool HasName(string name) => Tag.Name == name;

        public XmlElement GetChildWithAttribute(
            string name,
            string attributeName,
            string attributeValue)
        {
            if (children == null)
                return null;
            for (int i = 0; i < children.Count; i++)
                if (children[i].HasName(name) &&
                    children[i].Tag.GetAttributeValue(attributeName) == attributeValue)
                    return children[i];
            return null;
        }

        public XmlElement GetChild(int index)
        {
            if (children == null || children.Count <= index)
                return null;
            return children[index];
        }

        public XmlElement GetChild(string name)
        {
            if (children == null)
                return null;
            for (int i = 0; i < children.Count; i++)
                if (children[i].HasName(name))
                    return children[i];
            return null;
        }

        public void AddData(XmlDataEntry dataEntry) => data.Add(dataEntry);
        public void AddChild(XmlElement child) => children.Add(child);

        public override string ToString()
            => $"(Name: {Tag.Name}, Data: {data.Count}, Children: {children.Count})";
    }

    public sealed class XmlDocument
    {
        //Properties
        public IReadOnlyList<XmlElement> Elements => elements;
        public int RootElementCount => elements.Count;

        //Data
        private readonly List<XmlElement> elements = new List<XmlElement>();

        public void AddElement(XmlElement element) => elements.Add(element);
    }

    /// <summary>
    /// Basic xml parser / tokenizer, can be used as the tokenizer in a multi pass parser
    /// Followed basic spec from wiki: https://en.wikipedia.org/wiki/XML
    /// </summary>
    public sealed class XmlParser : IParser<XmlDocument>
    {
        private enum TagType : byte
        {
            ElementStart = 0, //Example: <ELEMNAME>
            ElementEnd = 1, //Example: </ELEMNAME>
            ElementSelfClose = 2, //Example: <ELEMNAME/>
            ProcessingInstruction = 3, //Example: <?ELEMNAME?>
            TypeDeclaration = 4, //Example: <!ELEMNAME>
            Comment = 5 //Example: <!-- ELEMNAME -->
        }

        private readonly TextParser par;    
        private readonly Stack<XmlElement> activeStack = new Stack<XmlElement>();
        private readonly XmlDocument document = new XmlDocument();

        private readonly ResizeArray<XmlAttribute> attributeCache = new ResizeArray<XmlAttribute>();
        
        public XmlParser(Stream inputStream, bool leaveStreamOpen = false)
            => par = new TextParser(inputStream, Encoding.UTF8, leaveStreamOpen);

        public XmlDocument Parse()
        {
            if (par.Current.IsEndOfFile)
                throw new Exception($"[{nameof(XmlParser)}] Stream allready at the end");
                
            while (!par.Current.IsEndOfFile)
            {
                par.ConsumeWhitespace(includeNewline: true); //Allow starting whitespace
                (XmlTag tag, TagType type) = ConsumeTag();
                switch (type)
                {
                    case TagType.Comment: break; //Currently not handling comments
                    case TagType.ElementEnd: //Element end should remove a element from the active-stack
                    {
                        if (activeStack.Count == 0 || activeStack.Peek().Tag.Name != tag.Name)
                            throw par.CreateError($"Closing tag for '{tag.Name}' found but its not open");
                        activeStack.Pop();
                        break;
                    }
                    default: //All other types should create a new element
                    {
                        XmlElement element = new XmlElement(tag);
                        if (activeStack.Count == 0) //If there are not active elements they we are a root element
                            document.AddElement(element);
                        else //Otherwise we become a child of the topmost element
                            activeStack.Peek().AddChild(element);

                        //If this was a element start (so not self-closing) then we add ourselves to the
                        //active stack until we find a close tag for us
                        if (type == TagType.ElementStart)
                            activeStack.Push(element);
                        break;
                    }
                }

                //Consume empty space between the tags
                par.ConsumeWhitespace(includeNewline: true);

                //If there is non xml-tag text between the tokens then we treat that as data entries
                //belonging to the active element
                if (!par.Current.IsCharacter('<') && !par.Current.IsEndOfFile)
                {
                    if (activeStack.Count == 0)
                        throw par.CreateError("Text found outside of root elements");

                    long startPosition = par.CurrentBytePosition;
                    par.ConsumeIgnoreUntil(() => par.Current.IsCharacter('<'));
                    long endPosition = par.CurrentBytePosition;

                    activeStack.Peek().AddData(new XmlDataEntry(startPosition, endPosition));
                }
            }
            if (activeStack.Count > 0)
                throw par.CreateError("Not all tags have been closed");            
            return document;
        }

        public void Dispose() => par.Dispose();

        private (XmlTag tag, TagType type) ConsumeTag()
        {
            par.ExpectConsume('<');
            bool isTypeDeclaration = par.TryConsume('!');
            //Comments are <!- and name of element cannot start with dash so this should be a safe
            //check to see if its a comment
            if (par.Current.IsCharacter('-')) 
            {
                string comment = ConsumeComment();
                par.ExpectConsume('>');
                return (new XmlTag(comment, attributes: null), TagType.Comment);
            }

            bool isClose = par.TryConsume('/');
            bool isProcessingInstruction = par.TryConsume('?');
            par.ConsumeWhitespace(); //Allow whitespace between tag start and tag name
            string tagName = par.ConsumeUntil(() => par.Current.IsWhitespace || par.Current.IsCharacter('>'));
            par.ConsumeWhitespace(); //Allow whitespace after tag name

            //While we are not at the end we keep reading attributes
            //(close tag are not allowed to have atribute)
            attributeCache.Clear();
            while (
                !par.Current.IsCharacter('>') &&
                !par.Current.IsCharacter('?') &&
                !par.Current.IsCharacter('/'))
            {
                string attributeName = par.ConsumeUntil(() => par.Current.IsCharacter('=') || par.Current.IsWhitespace);
                par.ConsumeWhitespace(); //Allow whitespace between name and '='
                par.ExpectConsume('=');
                par.ConsumeWhitespace(); //Allow whitespace between '=' and value
                string attributeValue = par.ConsumeQuotedString();
                par.ConsumeWhitespace(); //Allow whitespace after value
                attributeCache.Add(new XmlAttribute(attributeName, attributeValue));
            }
            if (isProcessingInstruction)
                par.ExpectConsume('?'); //Processing instructions have to end with ?>
            bool isSelfClose = par.TryConsume('/');
            par.ExpectConsume('>');

            return
            (
                new XmlTag(
                    name: tagName,
                    attributes: attributeCache.IsEmpty ? null : attributeCache.ToArray()),
                GetType()
            );

            TagType GetType()
            {
                if (isProcessingInstruction)
                    return TagType.ProcessingInstruction;
                if (isTypeDeclaration)
                    return TagType.TypeDeclaration;
                if (isClose)
                    return TagType.ElementEnd;
                if (isSelfClose)
                    return TagType.ElementSelfClose;
                return TagType.ElementStart;
            }

            string ConsumeComment()
            {
                par.ExpectConsume('-', count: 2);

                string result = par.ConsumeUntil(() =>
                    par.Current.IsCharacter('-') && par.Next.IsCharacter('-')); 

                par.ExpectConsume('-', count: 2);
                return result.Trim(); //Trim the whitespace from beginning and end of the comment
            }
        }
    }
}