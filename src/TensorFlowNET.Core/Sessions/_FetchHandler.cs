﻿using NumSharp.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tensorflow
{
    /// <summary>
    /// Handler for structured fetches.
    /// </summary>
    public class _FetchHandler<T>
    {
        private _ElementFetchMapper<T> _fetch_mapper;
        private List<Tensor> _fetches = new List<Tensor>();
        private List<bool> _ops = new List<bool>();
        private List<Tensor> _final_fetches = new List<Tensor>();
        private List<T> _targets = new List<T>();

        public _FetchHandler(Graph graph, T fetches, Dictionary<Tensor, NDArray> feeds = null, Action feed_handles = null)
        {
            _fetch_mapper = new _FetchMapper<T>().for_fetch(fetches);
            foreach(var fetch in _fetch_mapper.unique_fetches())
            {
                switch (fetch)
                {
                    case Operation val:
                        _assert_fetchable(graph, val);
                        _targets.Add((T)(object)val);
                        _ops.Add(true);
                        break;
                    case Tensor val:
                        _assert_fetchable(graph, val.op);
                        _fetches.Add(val);
                        _ops.Add(false);
                        break;
                }

            }

            _final_fetches = _fetches;
        }

        public NDArray build_results(BaseSession session, NDArray[] tensor_values)
        {
            var full_values = new List<object>();
            if (_final_fetches.Count != tensor_values.Length)
                throw new InvalidOperationException("_final_fetches mismatch tensor_values");

            int i = 0;
            int j = 0;
            foreach(var is_op in _ops)
            {
                if (is_op)
                {
                    full_values.Add(null);
                }
                else
                {
                    var value = tensor_values[j];
                    j += 1;
                    full_values.Add(value);
                }
                i += 1;
            }

            if (j != tensor_values.Length)
                throw new InvalidOperationException("j mismatch tensor_values");

            return _fetch_mapper.build_results(full_values);
        }

        private void _assert_fetchable(Graph graph, Operation op)
        {
            if (!graph.is_fetchable(op))
            {
                throw new Exception($"Operation {op.Name} has been marked as not fetchable.");
            }
        }

        public List<Tensor> fetches()
        {
            return _final_fetches;
        }

        public List<T> targets()
        {
            return _targets;
        }
    }
}
