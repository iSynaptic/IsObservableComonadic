using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using NUnit.Framework;

namespace IsObservableComonadic
{
    [TestFixture]
    public class ComonadicLawsTests
    {
        [Test]
        public void Law1()
        {
            AssertLaw1(Observable.Return("42"));
            AssertLaw1(Observable.Return<string>(null));
            AssertLaw1(Observable.Empty<string>());
            AssertLaw1(Observable.Throw<string>(new InvalidOperationException()));
        }

        private static void AssertLaw1(IObservable<string> wt)
        {
            var extend = getExtend<string, string>();
            var extract = getExtract<string>();

            var id = Curry(extend, extract);

            AssertSameResult(() => wt.ToEnumerable().ToArray(),
                             () => id(wt).ToEnumerable().ToArray(),
                             (l, r) => l.SequenceEqual(r));
        }

        [Test]
        public void Law2()
        {
            Func<IObservable<string>, string> wt2u = observable => observable.FirstOrDefault();

            AssertLaw2(wt2u, Observable.Return("42"));
            AssertLaw2(wt2u, Observable.Return<string>(null));
            AssertLaw2(wt2u, Observable.Empty<string>());
            AssertLaw2(wt2u, Observable.Throw<string>(new NotSupportedException()));
        }

        private static void AssertLaw2<T, U>(Func<IObservable<T>, U> wt2u, IObservable<T> wt)
        {
            var extend = getExtend<T, U>();
            var extract = getExtract<U>();

            var wt2wu = Curry(extend, wt2u);

            var sameAsWt2wu = Compose(extract, wt2wu);

            AssertSameResult(() => wt2u(wt),
                 () => sameAsWt2wu(wt),
                 (l, r) => EqualityComparer<U>.Default.Equals(l, r));
        }

        [Test]
        public void Law3()
        {
            Func<IObservable<string>, int> wt2u = observable => int.Parse(observable.FirstOrDefault());
            Func<IObservable<int>, double> wu2v = observable => ((double)observable.FirstOrDefault());

            AssertLaw3(wt2u, wu2v, Observable.Return("42"));
            AssertLaw3(wt2u, wu2v, Observable.Empty<string>());
            AssertLaw3(wt2u, wu2v, Observable.Throw<string>(new NotSupportedException()));
        }

        private static void AssertLaw3<T, U, V>(Func<IObservable<T>, U> wt2u, Func<IObservable<U>, V> wu2v, IObservable<T> wt)
        {
            var extendTU = getExtend<T, U>();
            var extendUV = getExtend<U, V>();
            var extendTV = getExtend<T, V>();

            var left = Compose(Curry(extendUV, wu2v), Curry(extendTU, wt2u));
            var right = Curry(extendTV, Compose(wu2v, Curry(extendTU, wt2u)));

            AssertSameResult(() => left(wt).ToEnumerable().ToArray(),
                             () => right(wt).ToEnumerable().ToArray(),
                             (l, r) => l.SequenceEqual(r));

        }

        #region Helper Functions

        public static IObservable<U> extend<T, U>(Func<IObservable<T>, U> wt2u, IObservable<T> wt)
        {
            return wt.ManySelect(wt2u);
        }

        public static T extract<T>(IObservable<T> wt)
        {
            return wt.First();
        }

        public static Func<Func<IObservable<T>, U>, IObservable<T>, IObservable<U>> getExtend<T, U>()
        {
            return extend<T, U>;
        }

        public static Func<IObservable<T>, T> getExtract<T>()
        {
            return extract<T>;
        }

        public static Func<TRet> Curry<T1, TRet>(Func<T1, TRet> func, T1 arg1)
        {
            return () => func(arg1);
        }

        public static Func<T2, TRet> Curry<T1, T2, TRet>(Func<T1, T2, TRet> func, T1 arg1)
        {
            return (t2) => func(arg1, t2);
        }

        public static Func<T1, TRet> Compose<T1, T2, TRet>(Func<T2, TRet> outer, Func<T1, T2> inner)
        {
            return t1 => outer(inner(t1));
        }

        public static void AssertSameResult<TResult>(Func<TResult> left, Func<TResult> right, Func<TResult, TResult, bool> comparer)
        {
            TResult leftResult = default(TResult);
            Exception leftResultException = null;

            TResult rightResult = default(TResult);
            Exception rightResultException = null;

            try { leftResult = left(); } catch (Exception ex) { leftResultException = ex; }
            try { rightResult = right(); } catch (Exception ex) { rightResultException = ex; }

            if (leftResultException != null || rightResultException != null)
            {
                Assert.IsNotNull(leftResultException);
                Assert.IsNotNull(rightResultException);

                Assert.IsInstanceOf(leftResultException.GetType(), rightResultException);
                Assert.AreEqual(leftResultException.Message, rightResultException.Message);
            }
            else
                Assert.IsTrue(comparer(leftResult, rightResult));
        }

        #endregion
    }
}
