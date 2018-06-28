using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using HT.Engine.Utils;

namespace HT.Engine.Parsing
{
    public struct XmlAttribute : IEquatable<XmlAttribute>
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

    public struct XmlTag : IEquatable<XmlTag>
    {
        public readonly string Name;
        public readonly XmlAttribute[] Attributes; //Null if no tags where set

        public XmlTag(string name, XmlAttribute[] attributes)
        {
            Name = name;
            Attributes = attributes;
        }

        public string GetAttributeValue(string attributeName)
        {
            if(Attributes == null)
                return null;
            for (int i = 0; i < Attributes.Length; i++)
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
            if (Attributes.Length != other.Attributes.Length)
                return false;
            for (int i = 0; i < Attributes.Length; i++)
                if (Attributes[i] != other.Attributes[i])
                    return false;
            return true;
        }

        public override int GetHashCode()
        {
            int hashcode = Name == null ? 1 : Name.GetHashCode();
            if (Attributes != null)
            {
                for (int i = 0; i < Attributes.Length; i++)
                    hashcode ^= Attributes[i].GetHashCode();
            }
            return hashcode;
        }

        public override string ToString()
            => $"(Name: {Name}, Attributes: {(Attributes == null ? 0 : Attributes.Length)})";
    }

    /// <summary>
    /// Pointers to start and ending offset positions in the file. Why not just store the raw string
    /// here to avoid having to do another pass over the file? Well some file format put huge
    /// amount of data in the elements (for example model data) and saving it all here in this
    /// structure would make this potentially take huge amounts of memory and with this approach
    /// you can keep working with this stream based approach where you can parse large files without
    /// needing to have the raw text all in memory at the same time.
    /// </summary>
    public struct XmlDataEntry
    {
        public readonly long StartPosition;
        public readonly long EndPosition;

        public XmlDataEntry(long startPosition, long endPosition)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
        }
    }

    public sealed class XmlElement
    {
        public readonly XmlTag Tag;
        public readonly List<XmlDataEntry> Data = new List<XmlDataEntry>();
        public readonly List<XmlElement> Children = new List<XmlElement>();

        public XmlElement(XmlTag tag) => Tag = tag;

        public override string ToString()
            => $"(Name: {Tag.Name}, Children: {Children.Count})";
    }

    public sealed class XmlDocument
    {
        public readonly XmlElement RootElement;

        public XmlDocument(XmlElement element) => RootElement = element;
    }

    /// <summary>
    /// Basic xml parser / tokenizer, can be used as the tokenizer in a multi pass parser
    /// Followed basic spec from wiki: https://en.wikipedia.org/wiki/XML
    /// </summary>
    public sealed class XmlParser : TextParser<XmlDocument>
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
    
        private readonly Stack<XmlElement> activeStack = new Stack<XmlElement>();
        private readonly List<XmlElement> rootElements = new List<XmlElement>();

        private readonly ResizeArray<XmlAttribute> attributeCache = new ResizeArray<XmlAttribute>();
        
        public XmlParser(Stream inputStream) : base(inputStream, Encoding.UTF8) { }

        protected override bool ConsumeToken()
        {
            ConsumeWhitespace(); //Allow starting whitespace
            (XmlTag tag, TagType type) = ConsumeTag();
            switch (type)
            {
                case TagType.Comment: break; //Currently not handling comments
                case TagType.ElementEnd: //Element end should remove a element from the active-stack
                {
                    if (activeStack.Count == 0 || activeStack.Peek().Tag.Name != tag.Name)
                        throw CreateError($"Closing tag for {tag.Name} found but its not open");
                    activeStack.Pop();
                    break;
                }
                default: //All other types should create a new element
                {
                    XmlElement element = new XmlElement(tag);
                    if (activeStack.Count == 0) //If there are not active elements they we are a root element
                        rootElements.Add(element);
                    else //Otherwise we become a child of the topmost element
                        activeStack.Peek().Children.Add(element);

                    //If this was a element start (so not self-closing) then we add ourselves to the
                    //active stack until we find a close tag for us
                    if (type == TagType.ElementStart)
                        activeStack.Push(element);
                    break;
                }
            }

            //Consume empty space between the tags
            ConsumeWhitespace();

            //If there is non xml-tag text between the token then we treat that as data entries
            //belonging to the active element
            if (!Current.IsCharacter('<'))
            {
                if (activeStack.Count == 0)
                    throw CreateError("Text found outside of root elements");

                long startPosition = CurrentPosition;
                SkipUntil(current => current.IsCharacter('<'));
                long endPosition = CurrentPosition;

                activeStack.Peek().Data.Add(new XmlDataEntry(startPosition, endPosition));
            }

            //true means keep parsing (we want to read tokens till the end of the file)
            return true; 
        }

        protected override XmlDocument Construct()
        {
            if (activeStack.Count > 0)
                throw CreateError("Not all tags have been closed");            
            return new XmlDocument(rootElements[1]);
        }

        private (XmlTag tag, TagType type) ConsumeTag()
        {
            ExpectConsume('<');
            bool isTypeDeclaration = TryConsume('!');
            //Comments are <!- and name of element cannot start with dash so this should be a safe
            //check to see if its a comment
            if (Current.IsCharacter('-')) 
            {
                string comment = ConsumeComment();
                ExpectConsume('>');
                return (new XmlTag(comment, attributes: null), TagType.Comment);
            }

            bool isClose = TryConsume('/');
            bool isProcessingInstruction = TryConsume('?');
            ConsumeWhitespace(); //Allow whitespace between tag start and tag name
            string tagName = ConsumeUntil(current => current.IsWhitespace || current.IsCharacter('>'));
            ConsumeWhitespace(); //Allow whitespace after tag name

            //While we are not at the end we keep reading attributes
            //(close tag are not allowed to have atribute)
            attributeCache.Clear();
            while (!Current.IsCharacter('>') && !Current.IsCharacter('?') && !Current.IsCharacter('/'))
            {
                string attributeName = ConsumeUntil(current => current.IsCharacter('=') || current.IsWhitespace);
                ConsumeWhitespace(); //Allow whitespace between name and '='
                ExpectConsume('=');
                ConsumeWhitespace(); //Allow whitespace between '=' and value
                string attributeValue = ConsumeQuotedString();
                ConsumeWhitespace(); //Allow whitespace after value
                attributeCache.Add(new XmlAttribute(attributeName, attributeValue));
            }
            if (isProcessingInstruction)
                ExpectConsume('?'); //Processing instructions have to end with ?>
            bool isSelfClose = TryConsume('/');
            ExpectConsume('>');

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
                ExpectConsume('-', count: 2);
                string result = ConsumeUntil(Current => Current.IsCharacter('-') && Next.IsCharacter('-')); 
                ExpectConsume('-', count: 2);
                return result.Trim(); //Trim the whitespace from beginning and end of the comment
            }
        }
    }
}