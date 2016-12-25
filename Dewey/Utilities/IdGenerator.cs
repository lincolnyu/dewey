using System.Collections.Generic;

namespace Dewey.Utilities
{
    public class IdGenerator : IIdGenerator
    {
        #region Nested types

        public class Range
        {
            public long Begin; // inclusive
            public long End; //inclusive

            public Range(long begin, long end = long.MaxValue)
            {
                Begin = begin;
                End = end;
            }
        }

        #endregion

        #region Fields

        private readonly LinkedList<Range> _holes = new LinkedList<Range>(); //unused parts

        #endregion

        #region Methods

        public long Generate()
        {
            if (_holes.Count == 0)
            {
                _holes.AddFirst(new Range(2));
                return 1;
            }

            var f = _holes.First.Value;
            var r = f.Begin;
            if (f.Begin == f.End)
            {
                _holes.RemoveFirst();
            }
            else
            {
                f.Begin++;
            }
            return r;
        }

        public bool Use(long id)
        {
            if (_holes.Count == 0)
            {
                if (id > 1)
                {
                    _holes.AddFirst(new Range(1, id-1));                    
                }
                _holes.AddFirst(new Range(id+1));                    

                return true;
            }

            // search from the back assuming the ids come in in ascending order

            for (var p = _holes.Last; p != null; p = p.Previous)
            {
                var v = p.Value;
                if (v.Begin <= id && v.End >= id)
                {
                    if (v.Begin == v.End)
                    {
                        _holes.Remove(p);
                        return true;
                    }
                    if (v.Begin == id)
                    {
                        v.Begin ++;
                        return true;
                    }
                    if (v.End == id)
                    {
                        v.End--;
                        return true;
                    }
                    v.End = id - 1;
                    var nn = new Range(id + 1, v.End);
                    _holes.AddAfter(p, nn);
                    return true;
                }
            }

            return false;
        }

        public bool Unuse(long id)
        {
            // TODO optimize the structure with binary tree

            for (var p = _holes.First; p != null; p = p.Next)
            {
                var v = p.Value;
                if (v.Begin > id)
                {
                    var last = p.Previous;
                    if (last != null)
                    {
                        if (last.Value.End >= id)
                        {
                            return false;
                        }
                        if (last.Value.End + 2 == v.Begin)
                        {
                            // merge
                            v.Begin = last.Value.Begin;
                            _holes.Remove(last);
                            return true;
                        }
                        if (last.Value.End + 1 == id)
                        {
                            last.Value.End = id;
                            return true;
                        }
                    }

                    if (v.Begin == id + 1)
                    {
                        v.Begin = id;
                        return true;
                    }
                    var nn = new Range(id, id);
                    _holes.AddBefore(p, nn);
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
