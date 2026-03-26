// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("it17f+Owk+GKXWXy2MuvMRQhBBaTRg/sfB1Jr4DCbY1dvhozwrHrcQt7PlCiP6JMxRQnHolrh5Ht1uQXHekCyBn0bWK/PRKqsnx+TF1NsuR7b0dtqS8bKVyI0hFted6Ft6DdhfP5+kTzVEtmV8o7I/n2YqwdEW+iE8yezGFU71wvCkZm33wd89mxas3wXrtT7ncTWC+PKjOifT1VeoYDCgRB+fW+9zIQj2neupONnwXHe62RoiEvIBCiISoioiEhILxGH16IPd7limuOlw5q14GHCt95aatLWVuPyhCiIQIQLSYpCqZoptctISEhJSAjpsRjh0d8j2fZGDtOilZ8CS5XvaEEItQaVTB0ick3jszZZD8q4Dz5yWo/IPf//XYAlyIjISAh");
        private static int[] order = new int[] { 9,12,3,10,13,8,10,8,8,12,11,12,12,13,14 };
        private static int key = 32;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
