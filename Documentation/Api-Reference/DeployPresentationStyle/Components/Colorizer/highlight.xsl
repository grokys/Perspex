<?xml version="1.0" encoding="ISO-8859-1"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
<xsl:output encoding="ISO-8859-1" indent="no" omit-xml-declaration="yes"/>

<xsl:template match="code">
<xsl:value-of select="text()" disable-output-escaping="yes" />
</xsl:template>

<xsl:template match="comment">
<span class="highlight-comment"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
</xsl:template>

<xsl:template match="blockcomment">
<span class="highlight-comment">/*<xsl:value-of select="text()" disable-output-escaping="yes" />*/</span>
</xsl:template>

<xsl:template match="fsblockcomment">
<span class="highlight-comment">(*<xsl:value-of select="text()" disable-output-escaping="yes" />*)</span>
</xsl:template>

<xsl:template match="cpp-linecomment">
<span class="highlight-comment">//<xsl:value-of select="text()" disable-output-escaping="yes" /></span>
</xsl:template>

<xsl:template match="sql-linecomment">
<span class="highlight-comment">--<xsl:value-of select="text()" disable-output-escaping="yes" /></span>
</xsl:template>

<xsl:template match="pshell-cmdlet">
<span class="highlight-pshell-cmdlet"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
</xsl:template>

<xsl:template match="namespace">
<span class="highlight-namespace"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
</xsl:template>

<xsl:template match="literal">
<span class="highlight-literal"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
</xsl:template>

<xsl:template match="number">
<span class="highlight-number"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
</xsl:template>

<xsl:template match="keyword">
<span class="highlight-keyword"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
</xsl:template>

<xsl:template match="preprocessor">
<span class="highlight-preprocessor"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
</xsl:template>

<xsl:template match="xml-value"><xsl:value-of select="text()" disable-output-escaping="yes" /></xsl:template>
<xsl:template match="xml-tag"><span class="highlight-xml-tag"><xsl:value-of select="text()" disable-output-escaping="yes" /></span></xsl:template>
<xsl:template match="xml-bracket"><span class="highlight-xml-bracket"><xsl:value-of select="text()" disable-output-escaping="yes" /></span></xsl:template>
<xsl:template match="xml-bracket-inline"><span class="highlight-xml-bracket-inline"><xsl:value-of select="text()" disable-output-escaping="yes" /></span></xsl:template>
<xsl:template match="xml-comment"><span class="highlight-xml-comment"><xsl:value-of select="text()" disable-output-escaping="yes"/></span></xsl:template>
<xsl:template match="xml-cdata">
	<span class="highlight-xml-bracket"><xsl:text>&lt;![CDATA[</xsl:text></span>
	<span class="highlight-xml-cdata"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
	<span class="highlight-xml-bracket"><xsl:text>]]&gt;</xsl:text></span>
</xsl:template>
<xsl:template match="xml-attribute-name"><span class="highlight-xml-attribute-name"><xsl:value-of select="text()" disable-output-escaping="yes" /></span></xsl:template>
<xsl:template match="xml-attribute-equal"><span class="highlight-xml-attribute-equal"><xsl:value-of select="text()" disable-output-escaping="yes" /></span></xsl:template>
<xsl:template match="xml-attribute-value"><span class="highlight-xml-attribute-value"><xsl:value-of select="text()" disable-output-escaping="yes" /></span></xsl:template>

<xsl:template match="parsedcode">
	<xsl:choose>
		<xsl:when test="@in-box[.=0]">
			<xsl:element name="span">
				<xsl:attribute name="class">highlight-inline</xsl:attribute>
				<xsl:apply-templates/>
			</xsl:element>
		</xsl:when>
		<xsl:otherwise>
			<xsl:apply-templates/>
		</xsl:otherwise>
	</xsl:choose>
</xsl:template>

<xsl:template match="/">
	<xsl:apply-templates/>
</xsl:template>

</xsl:stylesheet>
