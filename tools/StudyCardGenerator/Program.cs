using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

var output = Path.GetFullPath(args.FirstOrDefault() ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Data", "StudyCards", "study-cards.json"));
var cards = new List<Card>();
void Add(string subject, string domain, int grade, string type, string slug, string prompt, string answer, string explanation, params string[] tags) =>
    cards.Add(new($"{subject}-{domain}-{slug}", subject, domain, grade, type, prompt, answer, explanation, tags, [$"mext-{subject}-2017"]));

// 数学：数と式
Add("math","numbers",1,"term","absolute-value","絶対値とは何？","数直線上で0からその数までの距離。","距離なので絶対値は0以上になる。たとえば−5の絶対値は5。","正負の数","絶対値");
Add("math","numbers",1,"pitfall","negative-subtraction","−3−(−5)を計算すると？","2","負の数を引くことは、その数の反対の数を足すこと。−3＋5と考える。","正負の数","符号");
Add("math","numbers",1,"reasoning","distributive-law","3(x＋2)が3x＋6になるのはなぜ？","3を括弧内のxと2の両方へ掛けるから。","分配法則 a(b＋c)=ab＋ac を使っている。","文字式","分配法則");
Add("math","numbers",1,"reasoning","equation-balance","方程式の両辺に同じ数を加えてもよいのはなぜ？","等しい二つの量へ同じ操作をしても、等しい関係が保たれるから。","天びんの左右へ同じ重さを加えるイメージで考えられる。","一次方程式","等式");
Add("math","numbers",2,"pitfall","simultaneous-solution","連立方程式の解が満たす条件は？","二つの方程式を同時に満たすこと。","一方の式だけを満たす値は連立方程式の解ではない。","連立方程式","解");
Add("math","numbers",3,"term","square-root","√a（a＞0）はどんな数を表す？","二乗するとaになる正の数。","aの平方根は正負の二つで、√a自体はそのうち正の方を表す。","平方根","根号");
Add("math","numbers",3,"formula-unit","expansion-square","(a＋b)²の展開公式は？","a²＋2ab＋b²","中央の2abを忘れない。正方形の面積としても説明できる。","展開","公式");
Add("math","numbers",3,"reasoning","zero-product","(x−2)(x＋3)=0からx=2または−3と分かるのはなぜ？","積が0なら、少なくとも一方の因数が0だから。","零積の法則を使い、二つの一次方程式へ分けている。","二次方程式","因数分解");

// 数学：図形
Add("math","geometry",1,"diagram","perpendicular-bisector","線分ABの垂直二等分線上の点には、どんな性質がある？","点Aと点Bからの距離が等しい。","コンパスで同じ半径の円弧を描く基本作図の根拠になる。","作図","垂直二等分線");
Add("math","geometry",1,"formula-unit","prism-volume","柱体の体積を求める式は？","底面積×高さ","高さは底面に垂直な長さを使う。斜めの辺とは限らない。","空間図形","体積");
Add("math","geometry",2,"formula-unit","polygon-angle-sum","n角形の内角の和は？","180°×(n−2)","一つの頂点から対角線を引くと、n−2個の三角形に分けられる。","多角形","角");
Add("math","geometry",2,"term","congruence-sas","三角形の合同条件『二辺とその間の角』とは？","二組の辺と、その二辺にはさまれた角がそれぞれ等しいこと。","二辺と、間ではない角が等しいだけでは合同とは限らない。","合同","合同条件");
Add("math","geometry",2,"reasoning","proof-order","図形の証明で最初に整理する二つは？","仮定と結論","与えられた条件と、最終的に示すことを分けると必要な根拠を選びやすい。","証明","根拠");
Add("math","geometry",3,"pitfall","similar-area-ratio","相似比が2:3の図形の面積比は？","4:9","面積比は相似比の二乗、体積比は三乗になる。","相似","面積比");
Add("math","geometry",3,"formula-unit","inscribed-angle","同じ弧に対する円周角と中心角の関係は？","円周角は中心角の半分。","同じ弧を見ている角であることを確認して使う。","円","円周角");
Add("math","geometry",3,"formula-unit","pythagorean","直角三角形の三平方の定理は？","a²＋b²=c²（cは斜辺）","斜辺は直角の向かい側にあり、三辺のうち最も長い。","三平方の定理","直角三角形");

// 数学：関数
Add("math","functions",1,"term","function","yがxの関数であるとは？","xの値を決めると、対応するyの値がただ一つに決まること。","同じxに複数のyが対応する関係は関数ではない。","関数","変数");
Add("math","functions",1,"formula-unit","proportion","比例の式は？","y=ax","aは比例定数で、xが0でなければy÷xで求められる。","比例","比例定数");
Add("math","functions",1,"formula-unit","inverse-proportion","反比例の式は？","y=a/x、またはxy=a","xとyの積が一定になる関係として確認できる。","反比例","比例定数");
Add("math","functions",2,"term","slope","一次関数y=ax+bのaは何を表す？","変化の割合、グラフでは直線の傾き。","xの増加量に対するyの増加量の割合を表す。","一次関数","傾き");
Add("math","functions",2,"term","intercept","一次関数y=ax+bのbは何を表す？","y切片。x=0のときのyの値。","グラフがy軸と交わる点のy座標になる。","一次関数","切片");
Add("math","functions",2,"reasoning","intersection","二直線の交点が連立方程式の解になるのはなぜ？","交点の座標は二つの直線の式を同時に満たすから。","式の解とグラフ上の点を対応させている。","一次関数","連立方程式");
Add("math","functions",3,"pitfall","quadratic-change","y=ax²では変化の割合は一定？","一定ではない。","xが同じ量だけ増えても、yの増加量は場所によって変わる。","二次関数","変化の割合");
Add("math","functions",3,"diagram","quadratic-coefficient","y=ax²で|a|が大きくなると放物線はどうなる？","開き方が狭くなる。","aの符号は開く向きを、絶対値は開き方を決める。","二次関数","グラフ");

// 数学：データの活用
Add("math","data",1,"formula-unit","relative-frequency","相対度数を求める式は？","その階級の度数÷度数の合計","異なる大きさの集団でも割合として比較できる。","データ","相対度数");
Add("math","data",1,"diagram","histogram","ヒストグラムの横軸と縦軸は何を表す？","横軸は階級、縦軸は度数。","棒の間を空けず、連続する数値データの分布を表す。","ヒストグラム","分布");
Add("math","data",1,"reasoning","experimental-probability","試行回数を増やすと相対度数はどうなる？","ある一定の値に近づく傾向がある。","少ない回数ではばらつくため、結果が必ず理論値と一致するわけではない。","確率","相対度数");
Add("math","data",2,"diagram","tree-diagram","樹形図を使う目的は？","起こり得る場合を漏れや重複なく整理するため。","順序のある選択を枝分かれで表すと、全ての場合を数えやすい。","確率","樹形図");
Add("math","data",2,"formula-unit","complement","事象Aが起こらない確率は？","1−P(A)","全ての結果の確率の和が1であることを使う。","確率","余事象");
Add("math","data",2,"term","interquartile-range","四分位範囲とは？","第3四分位数−第1四分位数","中央の50%のデータがどれくらい散らばっているかを表す。","箱ひげ図","四分位範囲");
Add("math","data",2,"pitfall","box-plot-mean","箱ひげ図だけから平均値は分かる？","通常は分からない。","箱ひげ図が示すのは最小値、四分位数、中央値、最大値で、平均値とは別。","箱ひげ図","代表値");
Add("math","data",3,"reasoning","random-sampling","標本を無作為に選ぶのはなぜ？","特定の特徴をもつ人や物に偏るのを避けるため。","偏った標本では、母集団全体を正しく推測できない。","標本調査","無作為抽出");

// 理科：エネルギー
Add("science","energy",1,"term","reflection","光の反射で、入射角と反射角にはどんな関係がある？","入射角と反射角は等しい。","角度は鏡の面ではなく、鏡に垂直な法線から測る。","光","反射");
Add("science","energy",1,"diagram","convex-lens","凸レンズでスクリーンに映せる像を何という？","実像","物体が焦点より外側にあると、光が実際に集まる位置へ倒立した実像ができる。","光","凸レンズ");
Add("science","energy",1,"reasoning","sound-height","弦を短くすると音が高くなるのはなぜ？","弦の振動数が大きくなるから。","音の高さは振動の速さ、つまり振動数で決まる。","音","振動");
Add("science","energy",1,"formula-unit","pressure","圧力を求める式と単位は？","圧力＝力÷面積、単位はPa（パスカル）","同じ力でも、力が働く面積が小さいほど圧力は大きい。","力","圧力","Pa");
Add("science","energy",2,"formula-unit","ohms-law","電圧・電流・抵抗の関係は？","電圧＝電流×抵抗（V＝I×R）","単位は電圧V、電流A、抵抗Ωを使う。","電流","オームの法則");
Add("science","energy",2,"experiment","ammeter","電流計は回路へどのようにつなぐ？","測りたい部分に直列につなぐ。","電流計を並列につなぐと大きな電流が流れ、故障の原因になる。","回路","測定");
Add("science","energy",2,"formula-unit","electric-power","電力を求める式と単位は？","電力＝電圧×電流、単位はW（ワット）","1 Wは1秒あたり1 Jのエネルギーを使う割合を表す。","電力","W");
Add("science","energy",3,"reasoning","energy-conservation","摩擦がなければ、運動中の力学的エネルギーはどうなる？","位置エネルギーと運動エネルギーの和が一定に保たれる。","高さが下がると位置エネルギーが減り、その分運動エネルギーが増える。","運動","エネルギー");

// 理科：粒子
Add("science","particles",1,"term","density","密度とはどんな量？","一定体積あたりの質量","密度＝質量÷体積で、物質を見分ける手掛かりになる。","物質","密度");
Add("science","particles",1,"experiment","carbon-dioxide","二酸化炭素を確認する方法は？","石灰水を加え、白く濁るか確かめる。","二酸化炭素と石灰水が反応して炭酸カルシウムができる。","気体","実験");
Add("science","particles",1,"experiment","oxygen-collection","水に溶けにくい酸素を集めるのに適した方法は？","水上置換法","水と置き換えて集めるため、空気と混ざりにくい。","気体","実験");
Add("science","particles",1,"reasoning","state-change-mass","状態変化の前後で質量が変わらないのはなぜ？","物質をつくる粒子の種類と数が変わらないから。","粒子の間隔や並び方が変わっても、粒子そのものは保存される。","状態変化","粒子");
Add("science","particles",2,"term","atom-molecule","原子と分子の違いは？","原子は物質をつくる基本粒子、分子は複数の原子が結び付いた粒子。","分子をつくらない物質もあるため、両者を同じものとして扱わない。","原子","分子");
Add("science","particles",2,"diagram","reaction-equation","化学反応式の係数を合わせる理由は？","反応前後で原子の種類と数を等しくするため。","化学変化では原子がなくなったり新しく生じたりせず、組み替わる。","化学反応式","質量保存");
Add("science","particles",3,"term","ion","イオンとは何？","原子や原子の集まりが電気を帯びた粒子。","電子を失うと陽イオン、受け取ると陰イオンになる。","イオン","電子");
Add("science","particles",3,"reasoning","neutralization","酸とアルカリの中和で水をつくるイオンは？","水素イオンと水酸化物イオン","H⁺とOH⁻が結び付き、H₂Oになる。","酸","アルカリ","中和");

// 理科：生命
Add("science","life",1,"term","classification","生物を分類するとき、何を基準にする？","観察できる共通点と相違点","名前や印象ではなく、体のつくりや増え方など同じ観点で比べる。","分類","観察");
Add("science","life",1,"experiment","microscope","顕微鏡で最初に使う対物レンズは？","最も低倍率の対物レンズ","視野が広く明るいため、観察物を見つけやすい。","顕微鏡","観察");
Add("science","life",2,"diagram","plant-cell","植物細胞にあり、動物細胞にはない代表的なつくりは？","細胞壁、葉緑体、液胞","すべての植物細胞で葉緑体が目立つとは限らない。","細胞","植物");
Add("science","life",2,"term","photosynthesis","光合成で植物が取り入れる気体と放出する気体は？","二酸化炭素を取り入れ、酸素を放出する。","光エネルギーを使って養分をつくる過程である。","植物","光合成");
Add("science","life",2,"experiment","photosynthesis-control","光合成の実験で、葉の一部をアルミニウム箔で覆う目的は？","光がある場合とない場合を比較するため。","調べたい条件だけを変える対照実験にする。","光合成","対照実験");
Add("science","life",2,"reasoning","blood-circulation","小腸で吸収された養分が全身へ届くのはなぜ？","血液によって運ばれるから。","消化器官と循環器官は、物質の受渡しでつながっている。","消化","循環");
Add("science","life",3,"term","gene","遺伝子とは何？","形質を決めるもとになる遺伝情報の単位。","遺伝情報は染色体に含まれ、細胞分裂を通じて受け継がれる。","遺伝","染色体");
Add("science","life",3,"reasoning","food-web","食物網で生物が減ると他の生物にも影響するのはなぜ？","複数の生物が食べる・食べられる関係で結ばれているから。","一つの種の変化が、生態系全体の個体数や物質循環へ広がる。","生態系","食物網");

// 理科：地球
Add("science","earth",1,"term","epicenter","震源と震央の違いは？","震源は地震が始まった地下の地点、震央はその真上の地表の地点。","立体的な位置と地表上の位置を区別する。","地震","震源");
Add("science","earth",1,"diagram","p-s-wave","地震で先に到着する波は？","P波","P波による初期微動の後、S波による主要動が到着する。","地震波","初期微動");
Add("science","earth",1,"term","index-fossil","示準化石は何を知る手掛かり？","地層ができた年代","短い期間に広い範囲で栄えた生物の化石が適している。","地層","化石");
Add("science","earth",1,"reasoning","sediment-size","河口から遠いほど小さな粒が堆積しやすいのはなぜ？","水の流れが弱まり、小さな粒も沈むようになるから。","運搬する力と粒の大きさを結び付けて考える。","地層","堆積");
Add("science","earth",2,"formula-unit","humidity","湿度を求める式は？","湿度＝実際の水蒸気量÷飽和水蒸気量×100","気温によって飽和水蒸気量が変わる点に注意する。","気象","湿度");
Add("science","earth",2,"experiment","pressure","気圧を測る器具は？","気圧計","気圧・風向・気温など複数の観測値から天気を考える。","気象観測","気圧");
Add("science","earth",2,"reasoning","front-rain","前線付近で雨が降りやすいのはなぜ？","暖かい空気が押し上げられて冷え、水蒸気が凝結するから。","空気の上昇、温度低下、雲の発生を因果でつなぐ。","前線","雲");
Add("science","earth",3,"diagram","moon-phase","月の満ち欠けを決める三つの位置関係は？","太陽・地球・月の位置関係","月自身が光るのではなく、太陽に照らされた部分を地球から見ている。","月","天体");

// 社会：地理
Add("social-studies","geography",1,"map","latitude-longitude","緯度と経度はそれぞれ何を表す？","緯度は赤道から南北、経度は本初子午線から東西の位置。","緯線と経線を組み合わせると地球上の位置を表せる。","地図","位置");
Add("social-studies","geography",1,"term","scale","地図の縮尺とは？","実際の距離を地図上でどれだけ縮めたかを示す割合。","縮尺が大きい地図ほど、狭い範囲を詳しく表す。","地図","縮尺");
Add("social-studies","geography",1,"source","climograph","雨温図を読むとき最初に確認するものは？","気温と降水量の目盛り・単位、観測地点。","形だけで判断せず、数値と季節の違いを確かめる。","気候","資料");
Add("social-studies","geography",1,"reasoning","monsoon-life","季節風が人々の生活や産業に影響するのはなぜ？","季節によって風向や降水量が変わるから。","自然条件と農業・住居・災害対策を関係付ける。","気候","生活");
Add("social-studies","geography",2,"map","contour-lines","等高線の間隔が狭い場所はどんな斜面？","傾きが急な斜面","短い水平距離で高度が大きく変わることを表す。","地形図","等高線");
Add("social-studies","geography",2,"term","depopulation","過疎とはどんな状態？","人口流出などで地域人口が減り、生活や産業の維持が難しくなる状態。","単なる人口減少ではなく、地域機能への影響も考える。","人口","地域");
Add("social-studies","geography",2,"reasoning","industry-location","工場の立地を考える主な条件は？","原料、用水、交通、市場、労働力など。","産業の種類や時代によって重視される条件は変化する。","工業","立地");
Add("social-studies","geography",2,"source","population-pyramid","人口ピラミッドから読み取れることは？","年齢別・男女別の人口構成。","出生数や高齢化、過去の人口変化を考える手掛かりになる。","人口","資料");

// 社会：歴史
Add("social-studies","history",1,"chronology","century","西暦645年は何世紀？","7世紀","1年から100年までが1世紀で、年を100で割って切り上げる。","年代","世紀");
Add("social-studies","history",1,"term","ritsuryo","律令国家とはどのような国家？","律と令という法に基づき、中央集権的に統治する国家。","土地・人民や税の仕組みを中央政府が整えた。","古代","政治");
Add("social-studies","history",1,"cause-effect","samurai-rise","武士が成長した背景は？","地方の土地や治安を自ら守る必要が高まったこと。","荘園の拡大や地方政治の変化と結び付けて考える。","中世","武士");
Add("social-studies","history",1,"comparison","kamakura-muromachi","鎌倉幕府と室町幕府の共通点は？","武士を基盤に将軍が政治を行ったこと。","仕組みや支配の範囲、守護の役割の違いも比較する。","幕府","比較");
Add("social-studies","history",2,"chronology","unification","全国統一へ進んだ順番は？","織田信長、豊臣秀吉、徳川家康","人物名だけでなく、政策が次の政権へどう受け継がれたかを見る。","近世","統一");
Add("social-studies","history",2,"cause-effect","meiji-restoration","明治政府が中央集権化を進めた目的は？","国内を統一して近代国家をつくり、外国に対抗するため。","廃藩置県、徴兵令、地租改正などを目的と結ぶ。","明治維新","近代化");
Add("social-studies","history",2,"source","historical-source","歴史資料を読むとき、内容以外に確認することは？","誰が、いつ、どんな目的で作った資料か。","資料の立場や限界を確認して、分かることを判断する。","史料","信頼性");
Add("social-studies","history",3,"cause-effect","cold-war","冷戦とはどのような対立？","アメリカを中心とする陣営とソ連を中心とする陣営の対立。","大国同士の直接戦争を避けつつ、世界各地へ対立が広がった。","戦後","国際関係");

// 社会：公民
Add("social-studies","civics",3,"term","popular-sovereignty","国民主権とは？","国の政治を最終的に決める権力が国民にあること。","選挙や世論、政治参加を通して具体化される。","憲法","主権");
Add("social-studies","civics",3,"comparison","three-powers","三権分立で権力を分ける目的は？","権力の集中を防ぎ、互いに抑制・均衡させるため。","立法・行政・司法の関係として理解する。","政治","三権分立");
Add("social-studies","civics",3,"term","judicial-review","違憲審査権とは？","法令などが憲法に違反しないか裁判所が判断する権限。","憲法を最高法規として守る仕組みの一つ。","司法","憲法");
Add("social-studies","civics",3,"cause-effect","price","需要が増え、供給が変わらないと価格はどうなりやすい？","上がりやすい。","欲しい人が増える一方、商品の量が同じなら競争が強まる。","市場","価格");
Add("social-studies","civics",3,"term","social-security","社会保障の主な目的は？","生活上のリスクに社会全体で備え、生活の安定を支えること。","年金・医療・福祉など、受益と負担の両面を考える。","社会保障","財政");
Add("social-studies","civics",3,"comparison","direct-indirect-tax","直接税と間接税の違いは？","納める人と負担する人が同じか、異なるか。","所得税は直接税、消費税は間接税の代表例。","税","財政");
Add("social-studies","civics",3,"term","local-autonomy","地方自治が『民主主義の学校』と呼ばれるのはなぜ？","住民が身近な政治へ参加し、自治を経験できるから。","首長・議会の選挙、直接請求などの仕組みがある。","地方自治","政治参加");
Add("social-studies","civics",3,"reasoning","sustainability","持続可能な政策を考えるとき必要な視点は？","現在と将来、環境・経済・社会への影響を合わせて考えること。","一つの効果だけでなく、負担や副作用も比較する。","持続可能性","政策");

// 社会：資料と考察
Add("social-studies","inquiry",1,"source","graph-unit","統計グラフで最初に確認する三点は？","表題、単位、出典","何を、どの尺度で、誰が調べた資料かを確かめる。","統計","資料");
Add("social-studies","inquiry",1,"source","percentage-number","割合が増えていても実数が減ることはある？","ある。","全体の大きさが変われば、割合と実数は同じ動きをしない。","割合","統計");
Add("social-studies","inquiry",1,"chronology","background-trigger","歴史の『背景』と『きっかけ』の違いは？","背景は長期的な条件、きっかけは出来事を直接動かした契機。","原因を一つに決めず、時間の長さを分けて考える。","因果関係","歴史");
Add("social-studies","inquiry",2,"comparison","fair-comparison","二つの地域を公平に比較するには？","同じ指標・時期・単位を使う。","比較条件をそろえないと、差の原因を正しく判断できない。","比較","地理");
Add("social-studies","inquiry",2,"source","primary-secondary","一次資料と二次資料の違いは？","一次資料は当時の記録や現物、二次資料は後から分析・説明したもの。","どちらにも役割と限界があり、目的に応じて使い分ける。","史料","信頼性");
Add("social-studies","inquiry",3,"reasoning","multiple-perspectives","多面的・多角的に考えるとは？","複数の側面と異なる立場から事象を考えること。","政治・経済・社会・文化や、利益を受ける人と負担する人を分けて見る。","考察","立場");
Add("social-studies","inquiry",3,"source","correlation-causation","二つの数値が同時に増えれば、必ず因果関係がある？","必ずしもない。","共通する別の要因や偶然の一致もあるため、追加資料が必要。","統計","因果関係");
Add("social-studies","inquiry",3,"reasoning","claim-evidence","根拠ある説明の基本構成は？","主張・根拠・理由付け","資料の事実を示し、それがなぜ主張を支えるかを説明する。","記述","根拠");

var subjects = new[]
{
    new Subject("math","数学",[new Domain("numbers","数と式"),new Domain("geometry","図形"),new Domain("functions","関数"),new Domain("data","データの活用")],"https://www.mext.go.jp/component/a_menu/education/micro_detail/__icsFiles/afieldfile/2019/03/18/1387018_004.pdf"),
    new Subject("science","理科",[new Domain("energy","エネルギー"),new Domain("particles","粒子"),new Domain("life","生命"),new Domain("earth","地球")],"https://www.mext.go.jp/content/20230522-mxt_kyoikujinzai02-000033060_04.pdf"),
    new Subject("social-studies","社会",[new Domain("geography","地理"),new Domain("history","歴史"),new Domain("civics","公民"),new Domain("inquiry","資料と考察")],"https://www.mext.go.jp/component/a_menu/education/micro_detail/__icsFiles/afieldfile/2019/03/18/1387018_003.pdf")
};
var document = new Document(1,"2026.2","2026-09-20","ja-JP",new License("WakaRoute original study cards","Apache-2.0","https://www.apache.org/licenses/LICENSE-2.0","Copyright Receipt Roller, Inc."),subjects,"wakaroute-editorial",false,"学習指導要領の領域を参照し、教科書本文を転載せずに作成したワカルート独自の初版です。推奨学年は公式配当ではありません。",cards);
if (cards.Select(x=>x.Id).Distinct().Count()!=cards.Count) throw new InvalidDataException("Duplicate card IDs.");
if (subjects.Any(s=>s.Domains.Any(d=>cards.Count(c=>c.Subject==s.Id&&c.Domain==d.Id)<8))) throw new InvalidDataException("Every domain must have at least 8 cards.");
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
await File.WriteAllTextAsync(output, JsonSerializer.Serialize(document,new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase,WriteIndented=true,Encoder=JavaScriptEncoder.UnsafeRelaxedJsonEscaping})+Environment.NewLine,new UTF8Encoding(false));
Console.WriteLine($"Generated {cards.Count} cards: {string.Join(", ",cards.GroupBy(x=>x.Subject).Select(g=>$"{g.Key}={g.Count()}"))}");

record Document(int SchemaVersion,string DatasetVersion,string AsOf,string Language,License License,Subject[] Subjects,string Classification,bool OfficialGradeAssignment,string EditorialNote,List<Card> Items);
record License(string Name,string SpdxId,string Url,string Attribution);
record Subject(string Id,string Name,Domain[] Domains,string CurriculumSourceUrl);
record Domain(string Id,string Name);
record Card(string Id,string Subject,string Domain,int RecommendedGrade,string CardType,string Prompt,string Answer,string Explanation,string[] Tags,string[] SourceRefs);
