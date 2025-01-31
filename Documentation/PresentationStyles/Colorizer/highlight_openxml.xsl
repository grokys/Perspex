<?xml version="1.0" encoding="ISO-8859-1"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0"
                xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"
>
	<xsl:output encoding="ISO-8859-1" indent="no" omit-xml-declaration="yes"/>

	<!-- This is used to keep extra whitespace and line breaks intact -->
	<xsl:template match="text()">
		<w:r>
			<!-- Keep this on the same line to prevent extra space from getting included -->
			<w:t xml:space="preserve"><xsl:value-of select="." /></w:t>
		</w:r>
	</xsl:template>

	<xsl:template match="code">
		<w:r>
			<w:t xml:space="preserve"><xsl:value-of select="text()" disable-output-escaping="yes" /></w:t>
		</w:r>
	</xsl:template>

	<xsl:template match="comment">
		<span class="Comment"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
	</xsl:template>

	<xsl:template match="blockcomment">
		<span class="Comment">/*<xsl:value-of select="text()" disable-output-escaping="yes" />*/</span>
	</xsl:template>

	<xsl:template match="fsblockcomment">
		<span class="Comment">(*<xsl:value-of select="text()" disable-output-escaping="yes" />*)</span>
	</xsl:template>

	<xsl:template match="cpp-linecomment">
		<span class="Comment">//<xsl:value-of select="text()" disable-output-escaping="yes" /></span>
	</xsl:template>

	<xsl:template match="sql-linecomment">
		<span class="Comment">--<xsl:value-of select="text()" disable-output-escaping="yes" /></span>
	</xsl:template>

	<xsl:template match="pshell-cmdlet">
		<span class="PShellCmdlet"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
	</xsl:template>

	<xsl:template match="namespace">
		<span class="Namespace"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
	</xsl:template>

	<xsl:template match="literal">
		<span class="Literal"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
	</xsl:template>

	<xsl:template match="number">
		<span class="Number"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
	</xsl:template>

	<xsl:template match="keyword">
		<span class="Keyword"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
	</xsl:template>

	<xsl:template match="preprocessor">
		<span class="Preprocessor"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
	</xsl:template>

	<xsl:template match="xml-value">
		<w:r>
			<w:t xml:space="preserve"><xsl:value-of select="text()" disable-output-escaping="yes" /></w:t>
		</w:r>
	</xsl:template>

	<xsl:template match="xml-tag">
		<span class="XmlTag"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
	</xsl:template>

	<xsl:template match="xml-bracket">
		<span class="XmlBracket"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
	</xsl:template>

	<xsl:template match="xml-bracket-inline">
		<span class="XmlBracketInline"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
	</xsl:template>

	<xsl:template match="xml-comment">
		<span class="XmlComment"><xsl:value-of select="text()" disable-output-escaping="yes"/></span>
	</xsl:template>

	<xsl:template match="xml-cdata">
		<span class="XmlBracket"><xsl:text>&lt;![CDATA[</xsl:text></span>
		<span class="XmlCDATA"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
		<span class="XmlBracket"><xsl:text>]]&gt;</xsl:text></span>
	</xsl:template>

	<xsl:template match="xml-attribute-name">
		<span class="XmlAttributeName"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
	</xsl:template>

	<xsl:template match="xml-attribute-equal">
		<span class="XmlAttributeEqual"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
	</xsl:template>

	<xsl:template match="xml-attribute-value">
		<span class="XmlAttributeValue"><xsl:value-of select="text()" disable-output-escaping="yes" /></span>
	</xsl:template>

	<xsl:template match="parsedcode">
		<xsl:choose>
			<xsl:when test="@in-box[.=0]">
				<xsl:element name="span">
					<xsl:attribute name="class">CodeInline</xsl:attribute>
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
