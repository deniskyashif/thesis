using System;
using System.IO;

public class InputStream
{
    int len = 0;
    int pos = 0;
    char[] data;

    public InputStream(string source)
    {
        this.data = source.ToCharArray();
        this.len = source.Length;
    }

    public InputStream(char[] source)
    {
        this.data = source;
        this.len = source.Length;
    }

    public InputStream(TextReader source)
    {
        data = source.ReadToEnd().ToCharArray();
        len = data.Length;
    }

    public InputStream(Stream source) 
        : this(new StreamReader(source)) { }

    public bool IsExhausted => !(0 <= this.pos && this.pos < this.len);
    public int Pos => this.pos;
    public int Size => this.len;
    public void SetToStart() => this.pos = 0;
    public void SetToEnd() => this.pos = this.len - 1;
    public void MoveForward() => this.pos++;
    public void MoveBackward() => this.pos--;
    public char CharAt(int index) => this.data[index];

    public char Peek()
    {
        if (this.IsExhausted)
            throw new InvalidOperationException("Reached end of input.");

        return this.data[this.pos];
    }
}