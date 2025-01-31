<?xml version="1.0" encoding="ISO-8859-1"?>
<!-- This is used to convert the syntax elements to XAML flow document elements -->
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
<xsl:output encoding="ISO-8859-1" indent="no" omit-xml-declaration="yes"/>

<xsl:template match="code">
<xsl:value-of select="text()" disable-output-escaping="yes" />
</xsl:template>

<xsl:template match="comment">
<Span Style="{{DynamicResource HighlightComment}}"><xsl:value-of select="text()" disable-output-escaping="yes" /></Span>
</xsl:template>

<xsl:template match="blockcomment">
<Span Style="{{DynamicResource HighlightComment}}">/*<xsl:value-of select="text()" disable-output-escaping="yes" />*/</Span>
</xsl:template>

<xsl:template match="fsblockcomment">
<Span Style="{{DynamicResource HighlightComment}}">(*<xsl:value-of select="text()" disable-output-escaping="yes" />*)</Span>
</xsl:template>

<xsl:template match="cpp-linecomment">
<Span Style="{{DynamicResource HighlightComment}}">//<xsl:value-of select="text()" disable-output-escaping="yes" /></Span>
</xsl:template>

<xsl:template match="sql-linecomment">
<Span Style="{{DynamicResource HighlightComment}}">--<xsl:value-of select="text()" disable-output-escaping="yes" /></Span>
</xsl:template>

<xsl:template match="pshell-cmdlet">
<Span Style="{{DynamicResource HighlightPowerShellCmdLet}}"><xsl:value-of select="text()" disable-output-escaping="yes" /></Span>
</xsl:template>

<xsl:template match="namespace">
<Span Style="{{DynamicResource HighlightNamespace}}"><xsl:value-of select="text()" disable-output-escaping="yes" /></Span>
</xsl:template>

<xsl:template match="literal">
<Span Style="{{DynamicResource HighlightLiteral}}"><xsl:value-of select="text()" disable-output-escaping="yes" /></Span>
</xsl:template>

<xsl:template match="number">
<Span Style="{{DynamicResource HighlightNumber}}"><xsl:value-of select="text()" disable-output-escaping="yes" /></Span>
</xsl:template>

<xsl:template match="keyword">
<Span Style="{{DynamicResource HighlightKeyword}}"><xsl:value-of select="text()" disable-output-escaping="yes" /></Span>
</xsl:template>

<xsl:template match="preprocessor">
<Span Style="{{DynamicResource HighlightPreprocessor}}"><xsl:value-of select="text()" disable-output-escaping="yes" /></Span>
</xsl:template>

<xsl:template match="xml-value"><xsl:value-of select="text()" disable-output-escaping="yes" /></xsl:template>
<xsl:template match="xml-tag"><Span Style="{{DynamicResource HighlightXmlTag}}"><xsl:value-of select="text()" disable-output-escaping="yes" /></Span></xsl:template>
<xsl:template match="xml-bracket"><Span Style="{{DynamicResource HighlightXmlBracket}}"><xsl:value-of select="text()" disable-output-escaping="yes" /></Span></xsl:template>
<xsl:template match="xml-bracket-inline"><Span Style="{{DynamicResource HighlightXmlBracketInline}}"><xsl:value-of select="text()" disable-output-escaping="yes" /></Span></xsl:template>
<xsl:template match="xml-comment"><Span Style="{{DynamicResource HighlightXmlComment}}"><xsl:value-of select="text()" disable-output-escaping="yes"/></Span></xsl:template>
<xsl:template match="xml-cdata">
	<Span Style="{{DynamicResource HighlightXmlBracket}}"><xsl:text>&lt;![CDATA[</xsl:text></Span>
	<Span Style="{{DynamicResource HighlightXmlCData}}"><xsl:value-of select="text()" disable-output-escaping="yes" /></Span>
	<Span Style="{{DynamicResource HighlightXmlBracket}}"><xsl:text>]]&gt;</xsl:text></Span>
</xsl:template>
<xsl:template match="xml-attribute-name"><Span Style="{{DynamicResource HighlightXmlAttributeName}}"><xsl:value-of select="text()" disable-output-escaping="yes" /></Span></xsl:template>
<xsl:template match="xml-attribute-equal"><Span Style="{{DynamicResource HighlightXmlAttributeEqual}}"><xsl:value-of select="text()" disable-output-escaping="yes" /></Span></xsl:template>
<xsl:template match="xml-attribute-value"><Span Style="{{DynamicResource HighlightXmlAttributeValue}}"><xsl:value-of select="text()" disable-output-escaping="yes" /></Span></xsl:template>

<xsl:template match="/">
	<xsl:apply-templates/>
</xsl:template>

</xsl:stylesheet>
