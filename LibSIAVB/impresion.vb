Imports System.Data
Imports siaw_funciones

Public Class impresion

    Private ReadOnly funciones As New Funciones()
    Public Function imprimir_veremision(ByVal PathAplicacion As String, ByVal tabla As DataTable, ByVal titulo As String, ByVal empresa As String, ByVal usuario As String, ByVal nit As String, ByVal codvendedor As String, ByVal tdc As String, ByVal monedabase As String, ByVal codalmacen As String, ByVal fecha As String, ByVal telefono As String, ByVal ptoventa As String, ByVal codcliente As String, ByVal cliente As String, ByVal direccion As String, ByVal tipopago As String, ByVal subtotal As String, ByVal descuentos As String, ByVal recargos As String, ByVal totalimp As String, ByVal totalliteral As String, ByVal proforma As String, ByVal pesototal As String, ByVal dsctosdescrip As String, ByVal planpagos As String, ByVal flete As String, ByVal transporte As String, ByVal obs As String, ByVal preparacion As String, ByVal iva As String, ByVal facturacion As String, ByVal es_argentina As Boolean, ByVal nota_plan_pagos As String, ByVal aclaracion_direccion As String, ByVal nombre_comercial As String, ByVal codcliente_real As String, ByVal razonsocial As String, ByVal nit_cliente As String, ByVal complemento_nit_cliente As String, ByVal opcional As Boolean, ByVal es_contra_entrega As Boolean) As String
        Dim resultado As String = ""
        Dim rnd As New Random()
        Try
            Dim i, e As Integer
            '///generar archivo
            Dim nombarch As String = "r" & rnd.Next(1, 100000) & ".txt"
            'borrar archivo
            If System.IO.File.Exists(PathAplicacion & "\temp\" & nombarch) Then
                System.IO.File.Delete(PathAplicacion & "\temp\" & nombarch)
            End If
            'crear archivo y guardar primero el actual y luego los de la lista

            'Dim oFile As System.IO.File
            Dim oWrite As System.IO.StreamWriter
            'oWrite = New System.IO.StreamWriter(PathAplicacion & "\temp\" & nombarch, False, System.Text.Encoding.ASCII)
            oWrite = System.IO.File.CreateText(PathAplicacion & "\temp\" & nombarch)

            Dim fila, hoja As Integer
            hoja = 0
            For i = 0 To tabla.Rows.Count - 1
                fila = i + 1
                If (fila = 1) Then
                    '######################
                    '#  CABECERA DE HOJA  #
                    '######################
                    oWrite.WriteLine(empresa)
                    oWrite.WriteLine(nit)
                    oWrite.WriteLine(funciones.Rellenar(Now.Hour.ToString("00") & ":" & Now.Minute.ToString("00"), 8, " ", False) & funciones.CentrarCadena(titulo, 127, " "))
                    oWrite.WriteLine(funciones.Rellenar(proforma, 30, " ", False) & funciones.Rellenar(tipopago, 10, " ", False) & " ALMACEN: " & funciones.Rellenar(codalmacen, 10, " ", False))
                    If codcliente.Trim.Length = 0 Then
                        '//si codcliente es vacio es un cliente sin nombre y solo se le podra su nombre de cliente
                        oWrite.WriteLine(funciones.Rellenar(cliente, 30, " ", False) & " VENDEDOR: " & funciones.Rellenar(codvendedor, 7, " ", False) & " FACTOR: " & funciones.Rellenar(tdc, 7, " ", True) & funciones.Rellenar(monedabase, 4, " ", True) & " FECHA: " & funciones.Rellenar(fecha, 15, " ", False))
                    Else
                        oWrite.WriteLine(funciones.Rellenar(codcliente, 10, " ", False) & funciones.Rellenar(cliente, 30, " ", False) & " VENDEDOR: " & funciones.Rellenar(codvendedor, 7, " ", False) & " FACTOR: " & funciones.Rellenar(tdc, 7, " ", True) & funciones.Rellenar(monedabase, 4, " ", True) & " FECHA: " & funciones.Rellenar(fecha, 15, " ", False))
                    End If
                    oWrite.WriteLine("NOMBRE COMERCIAL: " & funciones.Rellenar(nombre_comercial, 50, " ", False))
                    oWrite.WriteLine(funciones.Rellenar(direccion, 40, " ", False) & " TELEFONO: " & funciones.Rellenar(telefono, 30, " ", False))
                    oWrite.WriteLine("PUNTO: " & funciones.Rellenar(ptoventa, 40, " ", False) & " PREPARACION: " & preparacion)

                    If es_argentina Then
                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        oWrite.WriteLine("NRO <-------DESCRIPCION--------->   UD        CANTIDAD        PESO          PRECIO       PRECIO       IMPORTE")
                        oWrite.WriteLine("                                                                             LISTA        UNIT.         TOTAL")
                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                    Else
                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        oWrite.WriteLine("NRO <-------DESCRIPCION--------->   UD  TP      CANTIDAD        PESO          PRECIO       PRECIO       IMPORTE")
                        oWrite.WriteLine("                                                                               LISTA        UNIT.         TOTAL")
                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                    End If

                    'oWrite.WriteLine("NNN_DDDDDDDDDDDD__MMMMMMMMMMMMMMM__UUU__PP__NNNNNNNNNNNN__NNNNNNNNNNNN__NNNNNNNNNNNN__NNNNNNNNNNN__NNNNNNNNNNNN")
                    'ElseIf (fila Mod 49 = 0) Then
                ElseIf (fila = ((hoja * 48) + 48 + 1)) Then
                    'es pie
                    '######################
                    '#  PIE DE HOJA       #
                    '######################
                    hoja = hoja + 1
                    oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                    oWrite.WriteLine("Hoja Nro. " & CStr(hoja) & "  (" & usuario & ")")
                    oWrite.WriteLine(" ")
                    oWrite.WriteLine(" ") 'recently added
                    'comienza la nueva hoja
                    oWrite.WriteLine(" ")
                    oWrite.WriteLine(" ")
                    'oWrite.WriteLine(" ") '& Convert.ToChar(27) & "K" & Convert.ToChar(5))
                    'regresar unos puntos para dejar ista la siguente hoja
                    '######################
                    '#  CABECERA DE HOJA  #
                    '######################
                    oWrite.WriteLine(empresa)
                    oWrite.WriteLine(nit)
                    oWrite.WriteLine(funciones.CentrarCadena(titulo, 135, " "))
                    oWrite.WriteLine(funciones.Rellenar(proforma, 30, " ", False) & funciones.Rellenar(tipopago, 10, " ", False) & " ALMACEN: " & funciones.Rellenar(codalmacen, 10, " ", False))
                    If codcliente.Trim.Length = 0 Then
                        '//si codcliente es vacio es un cliente sin nombre y solo se le podra su nombre de cliente
                        oWrite.WriteLine(funciones.Rellenar(cliente, 30, " ", False) & " VENDEDOR: " & funciones.Rellenar(codvendedor, 7, " ", False) & " FACTOR: " & funciones.Rellenar(tdc, 7, " ", True) & funciones.Rellenar(monedabase, 4, " ", True) & " FECHA: " & funciones.Rellenar(fecha, 15, " ", False))
                    Else
                        oWrite.WriteLine(funciones.Rellenar(codcliente, 10, " ", False) & funciones.Rellenar(cliente, 30, " ", False) & " VENDEDOR: " & funciones.Rellenar(codvendedor, 7, " ", False) & " FACTOR: " & funciones.Rellenar(tdc, 7, " ", True) & funciones.Rellenar(monedabase, 4, " ", True) & " FECHA: " & funciones.Rellenar(fecha, 15, " ", False))
                    End If

                    oWrite.WriteLine("NOMBRE COMERCIAL: " & funciones.Rellenar(nombre_comercial, 50, " ", False))
                    oWrite.WriteLine(funciones.Rellenar(direccion, 40, " ", False) & " TELEFONO: " & funciones.Rellenar(telefono, 30, " ", False))
                    oWrite.WriteLine("PUNTO: " & funciones.Rellenar(ptoventa, 40, " ", False))
                    If es_argentina Then
                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        oWrite.WriteLine("NRO <-------DESCRIPCION--------->   UD        CANTIDAD        PESO          PRECIO       PRECIO       IMPORTE")
                        oWrite.WriteLine("                                                                             LISTA        UNIT.         TOTAL")
                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                    Else
                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        oWrite.WriteLine("NRO <-------DESCRIPCION--------->   UD  TP      CANTIDAD        PESO          PRECIO       PRECIO       IMPORTE")
                        oWrite.WriteLine("                                                                               LISTA        UNIT.         TOTAL")
                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                    End If

                End If
                'poner fila
                'oWrite.WriteLine("NNN_DDDDDDDDDDDD__MMMMMMMMMMMMMMM__UUU__PP__NNNNNNNNNNNN__NNNNNNNNNNNN__NNNNNNNNNNNN__NNNNNNNNNNN__NNNNNNNNNNNN")
                If es_argentina Then
                    oWrite.WriteLine(funciones.Rellenar(fila.ToString(), 3, " ") & " " & funciones.Rellenar(tabla.Rows(i)("descripcion"), 12, " ", False) & "  " & funciones.Rellenar(tabla.Rows(i)("medida"), 15, " ", False) & "  " & funciones.Rellenar(tabla.Rows(i)("udm"), 3, " ", False) & "    " & funciones.Rellenar(CDbl(tabla.Rows(i)("cantidad")).ToString("###,###,##0.00"), 12, " ") & "  " & funciones.Rellenar(CDbl(tabla.Rows(i)("peso")).ToString("###,###,##0.00"), 12, " ") & "  " & funciones.Rellenar(CDbl(tabla.Rows(i)("preciolista")).ToString("###,###,##0.000000"), 12, " ") & "  " & funciones.Rellenar(CDbl(tabla.Rows(i)("precioneto")).ToString("###,###,##0.000000"), 12, " ") & "  " & funciones.Rellenar(CDbl(tabla.Rows(i)("total")).ToString("###,###,##0.00"), 12, " ") & " ......................")
                Else
                    oWrite.WriteLine(funciones.Rellenar(fila.ToString(), 3, " ") & " " & funciones.Rellenar(tabla.Rows(i)("descripcion"), 12, " ", False) & "  " & funciones.Rellenar(tabla.Rows(i)("medida"), 15, " ", False) & "  " & funciones.Rellenar(tabla.Rows(i)("udm"), 3, " ", False) & "  " & funciones.Rellenar(Convert.ToString(tabla.Rows(i)("codtarifa")), 2, " ") & "  " & funciones.Rellenar(CDbl(tabla.Rows(i)("cantidad")).ToString("###,###,##0.00"), 12, " ") & "  " & funciones.Rellenar(CDbl(tabla.Rows(i)("peso")).ToString("###,###,##0.00"), 12, " ") & "  " & funciones.Rellenar(CDbl(tabla.Rows(i)("preciolista")).ToString("###,###,##0.000000"), 12, " ") & "  " & funciones.Rellenar(CDbl(tabla.Rows(i)("precioneto")).ToString("###,###,##0.000000"), 12, " ") & "  " & funciones.Rellenar(CDbl(tabla.Rows(i)("total")).ToString("###,###,##0.00"), 12, " ") & " ......................")
                End If


                If (fila = tabla.Rows.Count) Then
                    'SI NO CABE EN ESA HOJA AÑADIR ESPACIOS HASTA SIGUIENTE HOJA PIE Y CABECERA DE HOJA Y RECIEN PONER.
                    If (fila - 1 - (hoja * 48)) > (48 - 44) Then
                        'no cabe rellenar con espacios y poner en otra hoja
                        For e = 0 To (48 - (fila - (hoja * 48))) - 1
                            oWrite.WriteLine(funciones.Rellenar((fila + e + 1).ToString(), 3, " ") & " --------")
                        Next

                        '######################
                        '#  PIE DE HOJA       #
                        '######################
                        hoja = hoja + 1
                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        oWrite.WriteLine("Hoja Nro. " & CStr(hoja) & "  (" & usuario & ")")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        'comienza la nueva hoja
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ") 'recently added
                        'oWrite.WriteLine(" ") '& Convert.ToChar(27) & "K" & Convert.ToChar(5))
                        'regresar unos puntos para dejar ista la siguente hoja
                        '######################
                        '#  CABECERA DE HOJA  #
                        '######################
                        oWrite.WriteLine(empresa)
                        oWrite.WriteLine(nit)
                        oWrite.WriteLine(funciones.CentrarCadena(titulo, 135, " "))
                        oWrite.WriteLine(funciones.Rellenar(proforma, 30, " ", False) & funciones.Rellenar(tipopago, 10, " ", False) & " ALMACEN: " & funciones.Rellenar(codalmacen, 10, " ", False))
                        If codcliente.Trim.Length = 0 Then
                            '//si codcliente es vacio es un cliente sin nombre y solo se le podra su nombre de cliente
                            oWrite.WriteLine(funciones.Rellenar(cliente, 30, " ", False) & " VENDEDOR: " & funciones.Rellenar(codvendedor, 7, " ", False) & " FACTOR: " & funciones.Rellenar(tdc, 7, " ", True) & funciones.Rellenar(monedabase, 4, " ", True) & " FECHA: " & funciones.Rellenar(fecha, 15, " ", False))
                        Else
                            oWrite.WriteLine(funciones.Rellenar(codcliente, 10, " ", False) & funciones.Rellenar(cliente, 30, " ", False) & " VENDEDOR: " & funciones.Rellenar(codvendedor, 7, " ", False) & " FACTOR: " & funciones.Rellenar(tdc, 7, " ", True) & funciones.Rellenar(monedabase, 4, " ", True) & " FECHA: " & funciones.Rellenar(fecha, 15, " ", False))
                        End If

                        oWrite.WriteLine("NOMBRE COMERCIAL: " & funciones.Rellenar(nombre_comercial, 50, " ", False))
                        oWrite.WriteLine(funciones.Rellenar(direccion, 40, " ", False) & " TELEFONO: " & funciones.Rellenar(telefono, 30, " ", False))
                        oWrite.WriteLine("PUNTO: " & funciones.Rellenar(ptoventa, 40, " ", False))
                        If es_argentina Then
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                            oWrite.WriteLine("NRO <-------DESCRIPCION--------->   UD        CANTIDAD        PESO          PRECIO       PRECIO       IMPORTE")
                            oWrite.WriteLine("                                                                             LISTA        UNIT.         TOTAL")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        Else
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                            oWrite.WriteLine("NRO <-------DESCRIPCION--------->   UD  TP      CANTIDAD        PESO          PRECIO       PRECIO       IMPORTE")
                            oWrite.WriteLine("                                                                               LISTA        UNIT.         TOTAL")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        End If

                        'es pie final
                        '######################
                        '#  PIE DE HOJA FINAL #
                        '######################
                        'oWrite.WriteLine("NNN_CCCCCCCCC__DDDDDDDDDDDDDDDDDDDDDDDDDDD__MMMMMMMMMMMMMMMMMMMM__UUU__PP__NNNNNNNNNNNN__NNNNNNNNNNNN__NNNNNNNNNNNN__NNNNNNNNNNNN")
                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        oWrite.WriteLine("PESO TOTAL: " & funciones.Rellenar(CDbl(pesototal).ToString("###,##0.00"), 10, " ") & "  Kgrs.                                                      SUB TOTAL :  " & funciones.Rellenar(CDbl(subtotal).ToString("###,###,##0.00"), 15, " "))
                        oWrite.WriteLine("                                                                                   RECARGOS :  " & funciones.Rellenar(CDbl(recargos).ToString("###,###,##0.00"), 15, " "))
                        oWrite.WriteLine("                                                                                 DESCUENTOS :  " & funciones.Rellenar(CDbl(descuentos).ToString("###,###,##0.00"), 15, " "))

                        oWrite.WriteLine("                                                                                               ---------------")
                        oWrite.WriteLine("                                                                                      TOTAL :  " & funciones.Rellenar(CDbl(totalimp).ToString("###,###,##0.00"), 15, " "))
                        oWrite.WriteLine(totalliteral)
                        oWrite.WriteLine(dsctosdescrip)
                        oWrite.WriteLine(" ")


                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        If tipopago = "CREDITO" And es_contra_entrega = False Then
                            oWrite.WriteLine("                                       --------------------------            -------------------------       ")
                            oWrite.WriteLine("                                            PREPARADO POR                          REVISADO POR              ")
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine("                                 Aclaracion..........................  Aclaracion:.......................    ")
                        Else
                            oWrite.WriteLine("         --------------------------            -------------------------       ------------------------------------------")
                            oWrite.WriteLine("              PREPARADO POR                          REVISADO POR              Conformidad Recepcion Mercaderia y Factura")
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine("  Aclaracion..........................  Aclaracion:.......................Aclaracion:......................CI:.................")
                            oWrite.WriteLine("                                                                                      Fecha:...../....../......    ")
                        End If
                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        oWrite.WriteLine(" TRANSPORTE: " & funciones.Rellenar(transporte, 180, " ", False) & "   FLETE PAGADO POR: " & funciones.Rellenar(flete, 35, " ", False))
                        oWrite.WriteLine("  DIRECCION: " & funciones.Rellenar(direccion, 90, " ", False))
                        oWrite.WriteLine(" ACLARACION: " & aclaracion_direccion)
                        oWrite.WriteLine("OBSERVACION: " & funciones.Rellenar(obs, 100, " ", False))
                        oWrite.WriteLine("FACTURACION: " & funciones.Rellenar(facturacion, 50, " ", False))
                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")


                        If tipopago = "CREDITO" And es_contra_entrega = False Then
                            'If sia_funciones.Cliente.Instancia.EsEmpresa(codcliente, opcional) Then
                            'oWrite.WriteLine("Yo: " & funciones.Rellenar(razonsocial, 54, ".", False) & " a traves de este pagare, de manera incondicional me obligo a realizar el(los)")
                            'Else
                            'oWrite.WriteLine("Yo: " & funciones.Rellenar(razonsocial, 54, ".", False) & " a traves de este pagare, de manera incondicional me obligo a realizar el(los)")
                            'End If
                            ' Antes era asi ahora solo va:
                            oWrite.WriteLine("Yo: " & funciones.Rellenar(razonsocial, 54, ".", False) & " a traves de este pagare, de manera incondicional me obligo a realizar el(los)")

                            oWrite.WriteLine("pago(s) del importe señalado en esta nota de remision a la empresa PERTEC S.R.L. segun detalle continuo sin necesidad de protesto.")
                            oWrite.WriteLine("PLAN DE PAGOS ( " & nota_plan_pagos & " )")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                            oWrite.WriteLine(planpagos)
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                            oWrite.WriteLine("                                     La falta de pago motivara la exigibilidad por via ejecutiva.")
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine("                                                  ------------------------------------------")
                            oWrite.WriteLine("                                                  Conformidad Recepcion Mercaderia y Factura")
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine("                                 Aclaracion:......................CI:................. Fecha:...../....../.......")
                        Else
                            '//no imprime nada si es al contado
                        End If
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine("El peso por Item de esta nota de remision es referencial de acuerdo a tabla internacional segun normas Norteamericanas (Pulgadas) ")
                        '// OJO: este porcien de texto se modifico desde fecha: 04-10-2019
                        'oWrite.WriteLine("o Internacionales (Milimetros). En los conjuntos de pernos con tuerca (Hx Y Carr), el peso indicado en la nota de remision es por ")
                        'oWrite.WriteLine("el conjunto, sin embargo esta mercaderia generalmente es embalada por separado")
                        oWrite.WriteLine("o Internacionales (Milimetros).")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine("***************************************************************************************************************************************")
                        oWrite.WriteLine("**                                    FAVOR REVISAR TODA LA MERCADERIA Y LOS DATOS DE SU FACTURA.                                    **")
                        oWrite.WriteLine("**                                     PASADAS LAS 48 HRS NO SE ACEPTAN CAMBIOS NI DEVOLUCIONES.                                     **")
                        oWrite.WriteLine("***************************************************************************************************************************************")
                        'Para botar la hoja
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                    Else
                        'si cabe
                        'es pie final
                        '######################
                        '#  PIE DE HOJA FINAL #
                        '######################
                        'oWrite.WriteLine("NNN_CCCCCCCCC__DDDDDDDDDDDDDDDDDDDDDDDDDDD__MMMMMMMMMMMMMMMMMMMM__UUU__PP__NNNNNNNNNNNN__NNNNNNNNNNNN__NNNNNNNNNNNN__NNNNNNNNNNNN")
                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        oWrite.WriteLine("PESO TOTAL: " & funciones.Rellenar(CDbl(pesototal).ToString("###,##0.00"), 10, " ") & "  Kgrs.                                                      SUB TOTAL :  " & funciones.Rellenar(CDbl(subtotal).ToString("###,###,##0.00"), 15, " "))
                        oWrite.WriteLine("                                                                                   RECARGOS :  " & funciones.Rellenar(CDbl(recargos).ToString("###,###,##0.00"), 15, " "))
                        oWrite.WriteLine("                                                                                 DESCUENTOS :  " & funciones.Rellenar(CDbl(descuentos).ToString("###,###,##0.00"), 15, " "))

                        oWrite.WriteLine("                                                                                               ---------------")
                        oWrite.WriteLine("                                                                                      TOTAL :  " & funciones.Rellenar(CDbl(totalimp).ToString("###,###,##0.00"), 15, " "))
                        oWrite.WriteLine(totalliteral)
                        oWrite.WriteLine(dsctosdescrip)
                        oWrite.WriteLine(" ")


                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        If tipopago = "CREDITO" And es_contra_entrega = False Then
                            oWrite.WriteLine("                                       --------------------------            -------------------------       ")
                            oWrite.WriteLine("                                            PREPARADO POR                          REVISADO POR              ")
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine("                                 Aclaracion..........................  Aclaracion:.......................    ")
                        Else
                            oWrite.WriteLine("         --------------------------            -------------------------       ------------------------------------------")
                            oWrite.WriteLine("              PREPARADO POR                          REVISADO POR              Conformidad Recepcion Mercaderia y Factura")
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine("  Aclaracion..........................  Aclaracion:.......................Aclaracion:......................CI:.................")
                            oWrite.WriteLine("                                                                                      Fecha:...../....../......    ")
                        End If

                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        oWrite.WriteLine(" TRANSPORTE: " & funciones.Rellenar(transporte, 180, " ", False) & "   FLETE PAGADO POR: " & funciones.Rellenar(flete, 35, " ", False))
                        oWrite.WriteLine("  DIRECCION: " & funciones.Rellenar(direccion, 90, " ", False))
                        oWrite.WriteLine(" ACLARACION: " & aclaracion_direccion)
                        oWrite.WriteLine("OBSERVACION: " & funciones.Rellenar(obs, 100, " ", False))
                        oWrite.WriteLine("FACTURACION: " & funciones.Rellenar(facturacion, 50, " ", False))
                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        If tipopago = "CREDITO" And es_contra_entrega = False Then

                            'If sia_funciones.Cliente.Instancia.EsEmpresa(codcliente, opcional) Then
                            'oWrite.WriteLine("Yo: " & funciones.Rellenar(razonsocial, 54, ".", False) & " a traves de este pagare, de manera incondicional me obligo a realizar el(los)")
                            'Else
                            'oWrite.WriteLine("Yo: " & funciones.Rellenar(razonsocial, 54, ".", False) & " a traves de este pagare, de manera incondicional me obligo a realizar el(los)")
                            'End If
                            ' Antes era asi ahora solo va:
                            oWrite.WriteLine("Yo: " & funciones.Rellenar(razonsocial, 54, ".", False) & " a traves de este pagare, de manera incondicional me obligo a realizar el(los)")

                            oWrite.WriteLine("pago(s) del importe señalado en esta nota de remision a la empresa PERTEC S.R.L. segun detalle continuo sin necesidad de protesto.")
                            oWrite.WriteLine("PLAN DE PAGOS ( " & nota_plan_pagos & " )")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                            oWrite.WriteLine(planpagos)
                            oWrite.WriteLine("                                     La falta de pago motivara la exigibilidad por via ejecutiva.")
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine("                                                  ------------------------------------------")
                            oWrite.WriteLine("                                                  Conformidad Recepcion Mercaderia y Factura")
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine("                                 Aclaracion:......................CI:................. Fecha:...../....../.......")
                        Else
                            '//no imprime nada si es al contado
                        End If
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine("El peso por Item de esta nota de remision es referencial de acuerdo a tabla internacional segun normas Norteamericanas (Pulgadas) ")
                        '// OJO: este porcien de texto se modifico desde fecha: 04-10-2019 POR INSTRUCCION DE JRA
                        'oWrite.WriteLine("o Internacionales (Milimetros). En los conjuntos de pernos con tuerca (Hx Y Carr), el peso indicado en la nota de remision es por ")
                        'oWrite.WriteLine("el conjunto, sin embargo esta mercaderia generalmente es embalada por separado")
                        oWrite.WriteLine("o Internacionales (Milimetros). ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine("***************************************************************************************************************************************")
                        oWrite.WriteLine("**                                    FAVOR REVISAR TODA LA MERCADERIA Y LOS DATOS DE SU FACTURA.                                    **")
                        oWrite.WriteLine("**                                     PASADAS LAS 48 HRS NO SE ACEPTAN CAMBIOS NI DEVOLUCIONES.                                     **")
                        oWrite.WriteLine("***************************************************************************************************************************************")

                        'Para botar la hoja
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                    End If
                End If
            Next
            'oWrite.WriteLine("123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345")
            oWrite.Close()
            resultado = PathAplicacion & "\temp\" & nombarch
            '################################################
            ' ver el largo del archivo y aumentar lineas en blanco para que quede para la siguiente hoja.
            ''################################################
            'Dim nrolineas As Integer = 0
            'Dim oFile2 As System.IO.File
            'Dim oRead As System.IO.StreamReader
            ''oWrite = New System.IO.StreamWriter(PathAplicacion & "\temp\" & nombarch, False, System.Text.Encoding.ASCII)
            'oRead = oFile.OpenText(PathAplicacion & "\temp\" & nombarch)
            'Dim linea As String
            'linea = oRead.ReadLine()
            'While Not linea Is Nothing
            '    nrolineas = nrolineas + 1
            '    linea = oRead.ReadLine()
            'End While
            'oRead.Close()
            'AUMENTAR LINEAS NECESARIAS

            '################################################

            'MANDAR IMPRESION
            'descomentar para imprimir
            'Me.imprimir_archivo(PathAplicacion & "\temp\", nombarch)

        Catch ex As Exception
            Console.WriteLine("Error: " & ex.Message)
            resultado = "error"
        End Try
        Return resultado

    End Function


    Public Function imprimir_inmovimiento(ByVal PathAplicacion As String, ByVal tabla As DataTable, ByVal titulo As String, ByVal tiponm As String, ByVal id_concepto As String, ByVal conceptodescripcion As String, ByVal empresa As String, ByVal usuario As String, ByVal nit As String, ByVal codvendedor As String, ByVal codalmacen As String, ByVal codalmacen_origen As String, ByVal codalmacen_destino As String, ByVal fecha As String, ByVal total As String, ByVal pesototal As String, ByVal obs As String, ByVal fecha_impresion As String, ByVal es_argentina As Boolean, ByVal es_ajuste As Boolean, ByVal nomCliente As String) As String
        'tipo_hoja 0 carta, 1 as sia_12x25
        Dim resultado As String = ""
        Dim cantidadxcosto As Double = 0
        Dim total_costo As Double = 0
        Dim rnd As New Random()
        Try
            Dim i, e As Integer
            '///generar archivo
            Dim nombarch As String = "p" & rnd.Next(1, 100000) & ".txt"
            'borrar archivo
            If System.IO.File.Exists(PathAplicacion & "\temp\" & nombarch) Then
                System.IO.File.Delete(PathAplicacion & "\temp\" & nombarch)
            End If
            'crear archivo y guardar primero el actual y luego los de la lista

            'Dim oFile As System.IO.File
            Dim oWrite As System.IO.StreamWriter
            'oWrite = New System.IO.StreamWriter(PathAplicacion & "\temp\" & nombarch, False, System.Text.Encoding.ASCII)
            oWrite = System.IO.File.CreateText(PathAplicacion & "\temp\" & nombarch)

            Dim fila, hoja As Integer
            hoja = 0
            For i = 0 To tabla.Rows.Count - 1
                fila = i + 1
                If (fila = 1) Then
                    '######################
                    '#  CABECERA DE HOJA  #
                    '######################
                    oWrite.WriteLine(empresa)
                    oWrite.WriteLine(nit)
                    oWrite.WriteLine(" ")
                    oWrite.WriteLine(funciones.CentrarCadena(tiponm, 115, " "))
                    oWrite.WriteLine(funciones.CentrarCadena(titulo, 115, " "))
                    oWrite.WriteLine(" ")
                    oWrite.WriteLine("CONCEPTO: " & id_concepto & " " & funciones.Rellenar(conceptodescripcion, 105, " ", False) & " FECHA: " & funciones.Rellenar(fecha, 15, " ", False))
                    oWrite.WriteLine("ALMACEN: " & funciones.Rellenar(codalmacen, 15, " ", False) & "ORIGEN: " & funciones.Rellenar(codalmacen_origen, 15, " ", False) & " DESTINO: " & funciones.Rellenar(codalmacen_destino, 45, " ", False) & " VENDEDOR: " & funciones.Rellenar(codvendedor, 25, " ", False))

                    If es_argentina Then
                        If es_ajuste Then
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                            oWrite.WriteLine("  # CODIGO    <------DESCRIPCION-------->  <------MEDIDA------>       CANTIDAD               UD       COSTO       TOTAL")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        Else
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                            oWrite.WriteLine("  # CODIGO    <------DESCRIPCION-------->  <------MEDIDA------>       CANTIDAD               UD  ")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        End If

                    Else
                        If es_ajuste Then
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                            oWrite.WriteLine("  # CODIGO    <------DESCRIPCION-------->  <------MEDIDA------>       CANTIDAD               UD       COSTO       TOTAL")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        Else
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                            oWrite.WriteLine("  # CODIGO    <------DESCRIPCION-------->  <------MEDIDA------>       CANTIDAD               UD  ")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        End If
                    End If

                    'oWrite.WriteLine("NNN_CCCCCCCCC__DDDDDDDDDDDDDDDDDDDDDDDDDDD__MMMMMMMMMMMMMMMMMMMM__UUU__NNNNNNNNNNNN_______________PP__NNNNNNNNNNNN__NNNNNNNNNNN")
                    'ElseIf (fila Mod 49 = 0) Then
                ElseIf (fila = ((hoja * 48) + 48 + 1)) Then
                    'es pie
                    '######################
                    '#  PIE DE HOJA       #
                    '######################
                    hoja = hoja + 1
                    oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                    oWrite.WriteLine("Hoja Nro. " & CStr(hoja) & "  (" & usuario & ") " & fecha_impresion)
                    oWrite.WriteLine(" ")
                    oWrite.WriteLine(" ") 'recently added
                    'comienza la nueva hoja
                    oWrite.WriteLine(" ")
                    oWrite.WriteLine(" ")
                    'oWrite.WriteLine(" ") '& Convert.ToChar(27) & "K" & Convert.ToChar(5))
                    'regresar unos puntos para dejar ista la siguente hoja
                    '######################
                    '#  CABECERA DE HOJA  #
                    '######################
                    oWrite.WriteLine(" ")
                    oWrite.WriteLine(" ")
                    oWrite.WriteLine(" ")
                    oWrite.WriteLine(funciones.CentrarCadena(tiponm, 115, " "))
                    oWrite.WriteLine(funciones.CentrarCadena(titulo, 115, " "))
                    oWrite.WriteLine(" ")
                    oWrite.WriteLine("CONCEPTO: " & id_concepto & " " & funciones.Rellenar(conceptodescripcion, 105, " ", False) & " FECHA: " & funciones.Rellenar(fecha, 15, " ", False))
                    oWrite.WriteLine("ALMACEN: " & funciones.Rellenar(codalmacen, 15, " ", False) & "ORIGEN: " & funciones.Rellenar(codalmacen_origen, 15, " ", False) & " DESTINO: " & funciones.Rellenar(codalmacen_destino, 45, " ", False) & " VENDEDOR: " & funciones.Rellenar(codvendedor, 25, " ", False))

                    If es_argentina Then
                        If es_ajuste Then
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                            oWrite.WriteLine("  # CODIGO    <------DESCRIPCION-------->  <------MEDIDA------>       CANTIDAD               UD       COSTO       TOTAL")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        Else
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                            oWrite.WriteLine("  # CODIGO    <------DESCRIPCION-------->  <------MEDIDA------>       CANTIDAD               UD  ")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        End If

                    Else
                        If es_ajuste Then
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                            oWrite.WriteLine("  # CODIGO    <------DESCRIPCION-------->  <------MEDIDA------>       CANTIDAD               UD       COSTO       TOTAL")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        Else
                            oWrite.WriteLine(" ")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                            oWrite.WriteLine("  # CODIGO    <------DESCRIPCION-------->  <------MEDIDA------>       CANTIDAD               UD  ")
                            oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        End If
                    End If

                    'oWrite.WriteLine("NNN_CCCCCCCCC__DDDDDDDDDDDDDDDDDDDDDDDDDDD__MMMMMMMMMMMMMMMMMMMM__UUU__NNNNNNNNNNNN_______________PP__NNNNNNNNNNNN__NNNNNNNNNNN")
                End If
                'poner fila detalle
                If es_argentina Then
                    If es_ajuste Then
                        cantidadxcosto = tabla.Rows(i)("cantidad") * CDbl(tabla.Rows(i)("costo")).ToString("###,###,##0.000000")
                        total_costo = total_costo + cantidadxcosto
                        oWrite.WriteLine(funciones.Rellenar(fila.ToString(), 3, " ") & " " & funciones.Rellenar(tabla.Rows(i)("coditem"), 9, " ", False) & "  " & funciones.Rellenar(tabla.Rows(i)("descripcion"), 27, " ", False) & "  " & funciones.Rellenar(tabla.Rows(i)("medida"), 20, " ", False) & "  " & funciones.Rellenar(CDbl(tabla.Rows(i)("cantidad")).ToString("###,###,##0.00"), 12, " ", True) & "  " & funciones.Rellenar(tabla.Rows(i)("udm"), 15, " ") & "  " & funciones.Rellenar(CDbl(tabla.Rows(i)("costo")).ToString("###,###,##0.0000"), 10, " ") & "  " & funciones.Rellenar(CDbl(cantidadxcosto).ToString("###,###,##0.00"), 10, " ") & "  ")
                    Else
                        oWrite.WriteLine(funciones.Rellenar(fila.ToString(), 3, " ") & " " & funciones.Rellenar(tabla.Rows(i)("coditem"), 9, " ", False) & "  " & funciones.Rellenar(tabla.Rows(i)("descripcion"), 27, " ", False) & "  " & funciones.Rellenar(tabla.Rows(i)("medida"), 20, " ", False) & "  " & funciones.Rellenar(CDbl(tabla.Rows(i)("cantidad")).ToString("###,###,##0.00"), 12, " ", True) & "  " & funciones.Rellenar(tabla.Rows(i)("udm"), 15, " ") & "  ___________    ")
                    End If

                Else
                    If es_ajuste Then
                        cantidadxcosto = tabla.Rows(i)("cantidad") * CDbl(tabla.Rows(i)("costo")).ToString("###,###,##0.000000")
                        total_costo = total_costo + cantidadxcosto
                        oWrite.WriteLine(funciones.Rellenar(fila.ToString(), 3, " ") & " " & funciones.Rellenar(tabla.Rows(i)("coditem"), 9, " ", False) & "  " & funciones.Rellenar(tabla.Rows(i)("descripcion"), 27, " ", False) & "  " & funciones.Rellenar(tabla.Rows(i)("medida"), 20, " ", False) & "  " & funciones.Rellenar(CDbl(tabla.Rows(i)("cantidad")).ToString("###,###,##0.00"), 12, " ", True) & "  " & funciones.Rellenar(tabla.Rows(i)("udm"), 15, " ") & "  " & funciones.Rellenar(CDbl(tabla.Rows(i)("costo")).ToString("###,###,##0.0000"), 10, " ") & "  " & funciones.Rellenar(CDbl(cantidadxcosto).ToString("###,###,##0.00"), 10, " ") & "  ")
                    Else
                        oWrite.WriteLine(funciones.Rellenar(fila.ToString(), 3, " ") & " " & funciones.Rellenar(tabla.Rows(i)("coditem"), 9, " ", False) & "  " & funciones.Rellenar(tabla.Rows(i)("descripcion"), 27, " ", False) & "  " & funciones.Rellenar(tabla.Rows(i)("medida"), 20, " ", False) & "  " & funciones.Rellenar(CDbl(tabla.Rows(i)("cantidad")).ToString("###,###,##0.00"), 12, " ", True) & "  " & funciones.Rellenar(tabla.Rows(i)("udm"), 15, " ") & "  ___________    ")
                    End If
                End If

                If (fila = tabla.Rows.Count) Then
                    'SI NO CABE EN ESA HOJA AÑADIR ESPACIOS HASTA SIGUIENTE HOJA PIE Y CABECERA DE HOJA Y RECIEN PONER.
                    If (fila - 1 - (hoja * 48)) > (48 - 14) Then
                        'no cabe rellenar con espacios y poner en otra hoja
                        For e = 0 To (48 - (fila - (hoja * 48))) - 1
                            oWrite.WriteLine(funciones.Rellenar((fila + e + 1).ToString(), 3, " ") & " --------")
                        Next
                        '######################
                        '#  PIE DE HOJA       #
                        '######################
                        hoja = hoja + 1
                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        oWrite.WriteLine("Hoja Nro. " & CStr(hoja) & "  (" & usuario & ") " & fecha_impresion)
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        'comienza la nueva hoja
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ") 'recently added
                        'oWrite.WriteLine(" ") '& Convert.ToChar(27) & "K" & Convert.ToChar(5))
                        'regresar unos puntos para dejar ista la siguente hoja
                        '######################
                        '#  CABECERA DE HOJA  #
                        '######################
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(funciones.CentrarCadena(tiponm, 115, " "))
                        oWrite.WriteLine(funciones.CentrarCadena(titulo, 115, " "))
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine("CONCEPTO: " & id_concepto & " " & funciones.Rellenar(conceptodescripcion, 105, " ", False) & " FECHA: " & funciones.Rellenar(fecha, 15, " ", False))
                        oWrite.WriteLine("ALMACEN: " & funciones.Rellenar(codalmacen, 15, " ", False) & "ORIGEN: " & funciones.Rellenar(codalmacen_origen, 15, " ", False) & " DESTINO: " & funciones.Rellenar(codalmacen_destino, 45, " ", False) & " VENDEDOR: " & funciones.Rellenar(codvendedor, 25, " ", False))

                        If es_argentina Then
                            If es_ajuste Then
                                oWrite.WriteLine(" ")
                                oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                                oWrite.WriteLine("  # CODIGO    <------DESCRIPCION-------->  <------MEDIDA------>       CANTIDAD               UD       COSTO       TOTAL")
                                oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                            Else
                                oWrite.WriteLine(" ")
                                oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                                oWrite.WriteLine("  # CODIGO    <------DESCRIPCION-------->  <------MEDIDA------>       CANTIDAD               UD  ")
                                oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                            End If

                        Else
                            If es_ajuste Then
                                oWrite.WriteLine(" ")
                                oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                                oWrite.WriteLine("  # CODIGO    <------DESCRIPCION-------->  <------MEDIDA------>       CANTIDAD               UD       COSTO       TOTAL")
                                oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                            Else
                                oWrite.WriteLine(" ")
                                oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                                oWrite.WriteLine("  # CODIGO    <------DESCRIPCION-------->  <------MEDIDA------>       CANTIDAD               UD  ")
                                oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                            End If
                        End If

                        'oWrite.WriteLine("NNN_CCCCCCCCC__DDDDDDDDDDDDDDDDDDDDDDDDDDD__MMMMMMMMMMMMMMMMMMMM__UUU__NNNNNNNNNNNN_______________PP__NNNNNNNNNNNN__NNNNNNNNNNN")
                        'es pie final
                        '######################
                        '#  PIE DE HOJA FINAL #
                        '######################
                        'oWrite.WriteLine("NNN_CCCCCCCCC__DDDDDDDDDDDDDDDDDDDDDDDDDDD__MMMMMMMMMMMMMMMMMMMM__UUU__NNNNNNNNNNNN_______________PP__NNNNNNNNNNNN__NNNNNNNNNNN")
                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        If es_ajuste Then
                            oWrite.WriteLine("PESO TOTAL: " & funciones.Rellenar(CDbl(pesototal).ToString("###,##0.00"), 10, " ") & " Kgrs.                         " & funciones.Rellenar(CDbl(total).ToString("###,###,##0.00"), 25, " ") & "  " & funciones.Rellenar(CDbl(total_costo).ToString("###,###,##0.00"), 39, " "))
                        Else
                            oWrite.WriteLine("PESO TOTAL: " & funciones.Rellenar(CDbl(pesototal).ToString("###,##0.00"), 10, " ") & " Kgrs.                         " & funciones.Rellenar(CDbl(total).ToString("###,###,##0.00"), 25, " "))
                        End If
                        oWrite.WriteLine("OBS: " & funciones.Rellenar(obs, 100, " ", False) & " ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine("                  ______________________            _____________________          ____________________ ")
                        oWrite.WriteLine("                      PREPARADOR POR                    REVISADO POR                  RECIBI CONFORME ")
                        oWrite.WriteLine("                                                                                   " & nomCliente)
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")

                    Else
                        'si cabe
                        'es pie final
                        '######################
                        '#  PIE DE HOJA FINAL #
                        '######################
                        'oWrite.WriteLine("NNN_CCCCCCCCC__DDDDDDDDDDDDDDDDDDDDDDDDDDD__MMMMMMMMMMMMMMMMMMMM__UUU__NNNNNNNNNNNN_______________PP__NNNNNNNNNNNN__NNNNNNNNNNN")
                        oWrite.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------")
                        If es_ajuste Then
                            oWrite.WriteLine("PESO TOTAL: " & funciones.Rellenar(CDbl(pesototal).ToString("###,##0.00"), 10, " ") & " Kgrs.                         " & funciones.Rellenar(CDbl(total).ToString("###,###,##0.00"), 25, " ") & "  " & funciones.Rellenar(CDbl(total_costo).ToString("###,###,##0.00"), 39, " "))
                        Else
                            oWrite.WriteLine("PESO TOTAL: " & funciones.Rellenar(CDbl(pesototal).ToString("###,##0.00"), 10, " ") & " Kgrs.                         " & funciones.Rellenar(CDbl(total).ToString("###,###,##0.00"), 25, " "))
                        End If
                        oWrite.WriteLine("OBS: " & funciones.Rellenar(obs, 100, " ", False) & " ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine("                  ______________________            _____________________          ____________________ ")
                        oWrite.WriteLine("                      PREPARADOR POR                    REVISADO POR                  RECIBI CONFORME ")
                        oWrite.WriteLine("                                                                                   " & nomCliente)
                        oWrite.WriteLine(" ")
                        oWrite.WriteLine(" ")
                    End If
                End If
            Next
            'oWrite.WriteLine("123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345")
            oWrite.Close()
            resultado = PathAplicacion & "\temp\" & nombarch
            'MANDAR IMPRESION
            ' Me.imprimir_archivo(PathAplicacion & "\temp\", nombarch)
        Catch ex As Exception
            resultado = "Error"
        End Try
        Return resultado
    End Function







End Class
