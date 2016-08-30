<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <xsl:output method="html"/>
  <xsl:template match="cruisecontrol">
        <xsl:for-each select="build">
          <xsl:for-each select="Tests">
            <br/>
            <span style="color:gray">Test Date:</span>&#160;<span style="color:black; font-weight:bold"/>
            <xsl:for-each select="@Date">
              <span style="color:black; font-weight:bold">
                <xsl:value-of select="."/>
              </span>
            </xsl:for-each>
            <br/>
            <br/>
            <xsl:variable name="messages" select="/cruisecontrol//Tests//Test" />
            <xsl:if test="count($messages) > 0">
              <xsl:variable name="error.messages" select="$messages[@Result='Failed' or @Result='Error'] | /cruisecontrol//Tests//test | /cruisecontrol//Tests//test" />
              <xsl:variable name="error.messages.count" select="count($error.messages)" />
              <xsl:variable name="warning.messages" select="$messages[@Result='Warning']" />
              <xsl:variable name="warning.messages.count" select="count($warning.messages)" />
              <xsl:variable name="passed.messages" select="$messages[@Result='Passed']" />
              <xsl:variable name="passed.messages.count" select="count($passed.messages)" />
              <xsl:variable name="total" select="count($error.messages) + count($warning.messages) + count($passed.messages)"/>
              <span style="color:black">Test List:</span>
              <span style="color:green">
                Passed:<xsl:value-of select="$passed.messages.count"/>
              </span>
              <span style="color:red">
                Error:<xsl:value-of select="$error.messages.count"/>
              </span>
              <span style="color:blue">
                Warning:<xsl:value-of select="$warning.messages.count"/>
              </span>
              <span style="color:black">
                Total:<xsl:value-of select="$total"/>
              </span>
            </xsl:if>
            <br/>
            <span style="color:black">
              Test Build Version:<xsl:value-of select="TestBuildVersion/@Version"/>
            </span>
            <br/>
            <xsl:variable name="buildFolderLocation" select="concat(substring-before(TestResultLocation/@Location, 'TestsResults'), 'Build')" />
            <span style="color:black">
              Test Build Folder: <a target="_blank" href="{$buildFolderLocation}"><xsl:value-of select="$buildFolderLocation" /></a>
            </span>
            <br/>
            <xsl:variable name="resultFolderLocation" select="TestResultLocation/@Location" />
            <span style="color:black">
              Test Result Folder: <a target="_blank" href="{$resultFolderLocation}"><xsl:value-of select="$resultFolderLocation" /></a>
            </span>
            <xsl:for-each select="Test">
              <xsl:if test="position()=1">
                <table border="1">
                  <tbody>
                    <xsl:for-each select="../Test">
                      <xsl:sort select="not(@Result = 'Error' or @Result = 'Failed')"/>
                      <xsl:sort select="not(@Result = 'Warning' or @Result = 'Ignored')"/>
                      <xsl:sort select="@Result"/>
                      <tr>
                        <td width="100%">
                          <span>
                            <xsl:attribute name="STYLE">
                              color
                              <xsl:choose>
                                <xsl:when test="@Result[.='Error']">:red</xsl:when>
                                <xsl:when test="@Result[.='Failed']">:red</xsl:when>
                                <xsl:when test="@Result[.='Passed']">:green</xsl:when>
                                <xsl:when test="@Result[.='Warning']">:blue</xsl:when>
                                <xsl:when test="@Result[.='Ignored']">:magenta</xsl:when>
                                <xsl:otherwise>:black</xsl:otherwise>
                              </xsl:choose>
                            </xsl:attribute>
                            <xsl:for-each select="@Name">
                              <xsl:value-of select="."/>
                            </xsl:for-each>
                            (Elapsed:<xsl:for-each select="@Elapsed">
                              <xsl:value-of select="."/>
                            </xsl:for-each>)
                            Application:<xsl:for-each select="@ApplicationName">
                              <xsl:value-of select="."/>
                            </xsl:for-each>
                            MachineName:<xsl:for-each select="@MachineName">
                              <xsl:value-of select="."/>
                            </xsl:for-each>
                            IP:<xsl:for-each select="@IP">
                              <xsl:value-of select="."/>
                            </xsl:for-each>
                          </span>
                          <xsl:for-each select="Message">
                            <br/>
                            <xsl:call-template name="br-replace">
                              <xsl:with-param name="word" select="."/>
                            </xsl:call-template>
                          </xsl:for-each>
                          <xsl:if test="@Result[.!='Passed']">
                            <br/>
                            <xsl:call-template name="br-replace">
                              <xsl:with-param name="word" select="Error/Message/."/>
                            </xsl:call-template>
                            <xsl:call-template name="br-replace">
                              <xsl:with-param name="word" select="Warning/Message/."/>
                            </xsl:call-template>
                            <xsl:call-template name="br-replace">
                              <xsl:with-param name="word" select="EasyTestError/Message/."/>
                            </xsl:call-template>
                            <br/>
                            <xsl:if test="@ApplicationViews[.!='']">
                              <xsl:if test="boolean(substring-before(@ApplicationViews,';'))">
                                <a target="_blank">
                                  <xsl:attribute name="href">
                                    <xsl:value-of select="../TestResultLocation/@Location"/>\<xsl:value-of select="substring-before(@ApplicationViews,';')"/>
                                  </xsl:attribute>
                                  <xsl:value-of select="substring-after(substring-before(@ApplicationViews,';'),'_View.')"/>
                                </a> -
                              </xsl:if>
                              <xsl:if test="boolean(substring-after(@ApplicationViews,';'))">
                                <a target="_blank">
                                  <xsl:attribute name="href">
                                    <xsl:value-of select="../TestResultLocation/@Location"/>\<xsl:value-of select="substring-before(substring-after(@ApplicationViews,';'),';')"/>
                                  </xsl:attribute>
                                  <xsl:value-of select="substring-before(substring-after(substring-after(@ApplicationViews,';'),'_View.'),';')"/>
                                </a> -
                                <xsl:if test="boolean(substring-after(substring-after(@ApplicationViews,';'),';'))">
                                  <xsl:if test="not(boolean(substring-before(substring-after(substring-after(@ApplicationViews,';'),';'),';')))">
                                    <a target="_blank">
                                      <xsl:attribute name="href">
                                        <xsl:value-of select="../TestResultLocation/@Location"/>\<xsl:value-of select="substring-after(substring-after(@ApplicationViews,';'),';')"/>
                                      </xsl:attribute>
                                      <xsl:value-of select="substring-after(substring-after(substring-after(@ApplicationViews,';'),';'),'_View.')"/>
                                    </a> -
                                  </xsl:if>
                                  <xsl:if test="boolean(substring-before(substring-after(substring-after(@ApplicationViews,';'),';'),';'))">
                                    <a target="_blank">
                                      <xsl:attribute name="href">
                                        <xsl:value-of select="../TestResultLocation/@Location"/>\<xsl:value-of select="substring-before(substring-after(substring-after(@ApplicationViews,';'),';'),';')"/>
                                      </xsl:attribute>
                                      <xsl:value-of select="substring-before(substring-after(substring-after(substring-after(@ApplicationViews,';'),';'),'_View.'),';')"/>
                                    </a> -
                                    <xsl:if test="boolean(substring-after(substring-after(substring-after(@ApplicationViews,';'),';'),';'))">
                                      <a target="_blank">
                                        <xsl:attribute name="href">
                                          <xsl:value-of select="../TestResultLocation/@Location"/>\<xsl:value-of select="substring-after(substring-after(substring-after(@ApplicationViews,';'),';'),';')"/>
                                        </xsl:attribute>
                                        <xsl:value-of select="substring-after(substring-after(substring-after(substring-after(@ApplicationViews,';'),';'),';'),'_View.')"/>
                                      </a> -
                                    </xsl:if>
                                  </xsl:if>
                                </xsl:if>
                                <xsl:if test="not(boolean(substring-before(substring-after(@ApplicationViews,';'),';')))">
                                  <a target="_blank">
                                    <xsl:attribute name="href">
                                      <xsl:value-of select="../TestResultLocation/@Location"/>\<xsl:value-of select="substring-after(@ApplicationViews,';')"/>
                                    </xsl:attribute>
                                    <xsl:value-of select="substring-after(substring-after(@ApplicationViews,';'),'_View.')"/>
                                  </a> -
                                </xsl:if>
                              </xsl:if>
                              <xsl:if test="not(boolean(substring-before(@ApplicationViews,';')))">
                                <a target="_blank">
                                  <xsl:attribute name="href">
                                    <xsl:value-of select="../TestResultLocation/@Location"/>\<xsl:value-of select="@ApplicationViews"/>
                                  </xsl:attribute>
                                  <xsl:value-of select="substring-after(@ApplicationViews,'_View.')"/>
                                </a> -
                              </xsl:if>
                            </xsl:if>
                            <a target="_blank">
                              <xsl:attribute name="href"><xsl:value-of select="../TestResultLocation/@Location"/>\<xsl:value-of select="@Name"/>.ets</xsl:attribute>script
                            </a>
                          </xsl:if>
                        </td>
                      </tr>
                    </xsl:for-each>
                  </tbody>
                </table>
              </xsl:if>
            </xsl:for-each>
            <br/>
          </xsl:for-each>
        </xsl:for-each>
        <xsl:for-each select="Errors">
          <xsl:for-each select="Error">
            <br>
              <xsl:for-each select="@Message">
                <xsl:value-of select="."/>
              </xsl:for-each>
            </br>
          </xsl:for-each>
        </xsl:for-each>
  </xsl:template>
  <xsl:template match="Test">
    <xsl:apply-templates/>
  </xsl:template>
  <xsl:template name="br-replace">
    <xsl:param name="word"/>
    <xsl:variable name="crD">
      <xsl:text>&#xD;</xsl:text>
    </xsl:variable>
    <xsl:variable name="crA">
      <xsl:text>&#xA;</xsl:text>
    </xsl:variable>
    <xsl:variable name="crAD">
      <xsl:text>
</xsl:text>
    </xsl:variable>
    <xsl:choose>
      <xsl:when test="contains($word,$crAD)">
        <xsl:value-of select="substring-before($word,$crAD)"/>
        <br/>
        <xsl:call-template name="br-replace">
          <xsl:with-param name="word" select="substring-after($word,$crAD)"/>
        </xsl:call-template>
      </xsl:when>
      <xsl:when test="contains($word,$crA)">
        <xsl:value-of select="substring-before($word,$crA)"/>
        <br/>
        <xsl:call-template name="br-replace">
          <xsl:with-param name="word" select="substring-after($word,$crA)"/>
        </xsl:call-template>
      </xsl:when>
      <xsl:when test="contains($word,$crD)">
        <xsl:value-of select="substring-before($word,$crD)"/>
        <br/>
        <xsl:call-template name="br-replace">
          <xsl:with-param name="word" select="substring-after($word,$crD)"/>
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$word"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
</xsl:stylesheet>
