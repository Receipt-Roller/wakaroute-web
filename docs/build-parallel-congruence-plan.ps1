$ErrorActionPreference = 'Stop'

$courseTitle = '平行線と合同'
$courseId = '1299f799eb7c450080237a805bf0b638'

function L($title, $role, $skill, $goal, $misconception, $minutes = 7) {
    [pscustomobject]@{
        title = $title
        role = $role
        primarySkillId = $skill
        goal = $goal
        misconception = $misconception
        estimatedMinutes = $minutes
    }
}

$sectionSpecs = @(
    [pscustomobject]@{
        title = '角と図形の記号を読み直そう'; sectionId = '2aee597992ab4dbca24603ecbd14222c'; existingOverviewId = '2d4033547adc4dd09dfc7cf05492c967'
        lessons = @(
            (L '角の名前を3文字で読もう' 'prerequisite' 'angle.notation.read' '角の頂点を中央に置き、3文字の記号から角を特定する' '中央の文字ではなく見た目の向きだけで角を決める' 6),
            (L '頂点・辺・角を記号で書こう' 'basic' 'geometry.symbol.write' '点・線分・角を、それぞれ正しい記号で書き分ける' '線分と直線、角と頂点の記号を混同する' 6),
            (L '平行・垂直の記号を読もう' 'concept' 'line.relation.notation' '平行記号と垂直記号を、二直線の関係として言葉に直す' '平行記号を等号、垂直記号を直角そのものと読む' 6),
            (L '図の印から等しい辺・角を読み取ろう' 'application' 'geometry.mark.interpret' '辺のしるしと角の弧から、等しい対象を対応させる' '同じ色や近い位置だけで等しいと判断する' 7),
            (L '角と図形の記号を診断しよう' 'mastery' 'geometry.notation.diagnose' '記号の誤読を頂点・対象・関係の三種類へ分けて直す' '記号の順序が違っても同じ対象だと決める' 7)
        )
    },
    [pscustomobject]@{
        title = '対頂角と一直線上の角'; sectionId = '7ebdac9e83bd4160a2f76389cf536db5'; existingOverviewId = '9c0877c8b4e046b4b06f9bb9075d1558'
        lessons = @(
            (L '一直線上の角の和180°を使おう' 'concept' 'angle.straight.sum' '一直線を半回転と捉え、隣り合う角の和を180°にする' '一直線上にない二角まで180°とする' 6),
            (L '対頂角が等しい理由を説明しよう' 'concept' 'angle.vertical.explain' '一直線上の角の和を使い、対頂角が等しい理由を説明する' '対頂角が等しいことを図の見た目だけで覚える' 7),
            (L '交差する直線から対頂角を見つけよう' 'basic' 'angle.vertical.identify' '共通の頂点をもち向かい合う二角を対頂角として選ぶ' '隣り合う角を対頂角と取り違える' 6),
            (L '方程式で交差点の角を求めよう' 'procedure' 'angle.vertical.equation' '対頂角または一直線上の角を式にして未知の角を求める' '等しい関係と180°の関係を同じ式にする' 8),
            (L '対頂角と一直線上の角を診断しよう' 'mastery' 'angle.vertical.diagnose' '角の位置から使う関係を選び、誤った式を修正する' '角度の数値だけを見て関係を後付けする' 7)
        )
    },
    [pscustomobject]@{
        title = '同位角と錯角'; sectionId = 'ddb674f0052842b5af9fea3ad6d42b28'; existingOverviewId = 'bfca1071e0b3470c86dceec4fa0e5bad'
        lessons = @(
            (L '横切る直線と8つの角を整理しよう' 'overview' 'transversal.angle.map' '二直線を横切る直線が作る8つの角を、二つの交点ごとに整理する' '一つの交点の対頂角と二交点の角の組を混同する' 6),
            (L '同じ位置にある同位角を見つけよう' 'basic' 'angle.corresponding.identify' '二つの交点で同じ位置にある角を同位角として選ぶ' '同じ向きに開いているだけで同位角とする' 7),
            (L '内側で交互にある錯角を見つけよう' 'basic' 'angle.alternate.identify' '二直線の内側で横切る直線の反対側にある角を錯角として選ぶ' '同じ側の内角を錯角とする' 7),
            (L '同位角と錯角を選び分けよう' 'application' 'angle.transversal.select' '角の位置を二つの判断軸で調べ、同位角か錯角かを選ぶ' '角の大きさが同じなら名称も同じと考える' 7),
            (L '同位角と錯角の読み取りを診断しよう' 'mastery' 'angle.transversal.diagnose' '対頂角・同位角・錯角の誤分類を交点数と位置から直す' '平行線でない図では同位角や錯角が存在しないと考える' 7)
        )
    },
    [pscustomobject]@{
        title = '平行線と角の関係'; sectionId = '58bbfd3835954602bda8dde7573ca50a'; existingOverviewId = 'ae5a2e60bffa453e82e95e8639c556bf'
        lessons = @(
            (L '平行線が作る角の全体像をつかもう' 'overview' 'parallel.angle.map' '平行線を横切るときの等しい角と和が180°の角を一枚の図で整理する' 'すべての角が等しいと考える' 6),
            (L '平行線の同位角が等しいことを使おう' 'basic' 'parallel.corresponding.use' '平行線の同位角が等しい性質で未知の角を求める' '平行の印を確認せず同位角を等しいとする' 7),
            (L '平行線の錯角が等しいことを使おう' 'basic' 'parallel.alternate.use' '平行線の錯角が等しい性質で未知の角を求める' '錯角の位置を確認せず離れた角を結ぶ' 7),
            (L '同じ側の内角の和180°を使おう' 'concept' 'parallel.co-interior.use' '平行線の同じ側の内角が補角になることを使う' '同じ側の内角も等しいとする' 7),
            (L '折れ線を含む角を平行線で求めよう' 'application' 'parallel.angle.chase' '補助の平行線を引き、同位角・錯角・一直線上の角をつないで求める' '一度に複数の角を飛ばして等しいとする' 9)
        )
    },
    [pscustomobject]@{
        title = '平行線であることを確かめよう'; sectionId = '07b1429aa24d4cc7b5d86f5ee7b2a7d6'; existingOverviewId = 'eac455c1d277457fab4a16beac6ec203'
        lessons = @(
            (L '平行線の性質の逆を理解しよう' 'concept' 'parallel.converse.explain' '平行なら角が等しい性質と、角が等しければ平行という逆を区別する' '命題とその逆がいつも両方正しいと考える' 7),
            (L '同位角が等しいことから平行を示そう' 'basic' 'parallel.converse.corresponding' '一組の同位角が等しいことを根拠に二直線の平行を示す' '同位角でない二角の等しさを使う' 7),
            (L '錯角が等しいことから平行を示そう' 'basic' 'parallel.converse.alternate' '一組の錯角が等しいことを根拠に二直線の平行を示す' '角の位置を示さず数値だけで平行とする' 7),
            (L '同じ側の内角の和から平行を示そう' 'basic' 'parallel.converse.cointerior' '同じ側の内角の和が180°であることから平行を示す' '和が180°になる任意の二角を使う' 7),
            (L '平行を示す条件を選び分けよう' 'mastery' 'parallel.converse.select' '図の与条件から三つの平行判定のうち使えるものを選ぶ' '結論の平行を前提として角の性質を使う' 8)
        )
    },
    [pscustomobject]@{
        title = '三角形の内角と外角'; sectionId = '6edb1845fa174727b84c6fcd20f517ba'; existingOverviewId = '06ccc91d7e76493fbd2f53edc3e6ab4f'
        lessons = @(
            (L '三角形の内角の和180°を平行線で説明しよう' 'concept' 'triangle.angle-sum.explain' '頂点を通る平行線を使い、三角形の内角の和が180°になる理由を説明する' '公式だけを覚え、平行線との接続を失う' 8),
            (L '三角形の残りの内角を求めよう' 'procedure' 'triangle.interior.solve' '二つの内角から残りの一角を求め、和で検算する' '180°から一角だけを引いて終える' 6),
            (L '外角は離れた二内角の和と説明しよう' 'concept' 'triangle.exterior.theorem' '外角が隣り合わない二内角の和になる理由を示す' '隣の内角を二内角の一つに含める' 7),
            (L '三角形の外角を使って角を求めよう' 'application' 'triangle.exterior.solve' '外角の性質と一直線上の角を選び分けて未知角を求める' '外角の式と内角和の式を混ぜる' 8),
            (L '三角形の内角・外角を診断しよう' 'mastery' 'triangle.angle.diagnose' '内角和・外角・一直線のどの関係を使うか選び、誤式を直す' '図の形が違うと180°の性質が変わると考える' 7)
        )
    },
    [pscustomobject]@{
        title = '多角形の内角・外角'; sectionId = 'b3d6bcf8de92453ab8b1d2d7357e1073'; existingOverviewId = 'a4e4749003394ee1a09d0e77c1b08276'
        lessons = @(
            (L '多角形を三角形に分けよう' 'concept' 'polygon.triangulate' '一つの頂点から対角線を引き、n角形をn-2個の三角形に分ける' '対角線の本数と三角形の個数を混同する' 7),
            (L '多角形の内角の和を式で求めよう' 'procedure' 'polygon.interior.sum' 'n角形の内角の和を180°×(n-2)で求める' 'nをそのまま180°に掛ける' 7),
            (L '多角形の外角の和360°を説明しよう' 'concept' 'polygon.exterior.sum' '一周の回転として、一つずつ取った外角の和が360°になることを説明する' '一つの頂点で複数の外角を数える' 7),
            (L '正多角形の一つの角を求めよう' 'application' 'polygon.regular.angle' '内角和または外角和を辺の数で割り、正多角形の一角を求める' '正多角形でない図でも等分する' 8),
            (L '多角形の角を総合診断しよう' 'mastery' 'polygon.angle.diagnose' '内角和・外角和・正多角形の条件を選び分けて未知量を求める' '内角と外角のどちらを求めたか確認しない' 8)
        )
    },
    [pscustomobject]@{
        title = '合同な図形とは'; sectionId = '8263f5ad4813497dbe3ae451931052ee'; existingOverviewId = '8bb86688b4ae46aa9f0aeb683beb7eb4'
        lessons = @(
            (L 'ぴったり重なる合同の意味をつかもう' 'concept' 'congruence.meaning' '移動・回転・裏返しでぴったり重なる図形を合同と判断する' '向きが同じでなければ合同でないと考える' 6),
            (L '対応する頂点・辺・角を見つけよう' 'basic' 'congruence.correspondence' '形の並びをたどり、二図形の対応する要素を正しく組にする' '近くに描かれた頂点どうしを対応させる' 7),
            (L '合同を記号で正しい順序に書こう' 'procedure' 'congruence.notation' '対応順をそろえて三角形の合同を記号で表す' '対応しない頂点順で合同記号を書く' 7),
            (L '合同から等しい辺と角を読み取ろう' 'application' 'congruence.properties' '合同な図形の対応する辺と角がそれぞれ等しいことを使う' '合同条件と合同後に分かる性質を混同する' 7),
            (L '合同と対応関係を診断しよう' 'mastery' 'congruence.diagnose' '合同の意味・対応順・等しい要素の誤りを分類して直す' '面積が等しいだけで合同とする' 7)
        )
    },
    [pscustomobject]@{
        title = '三辺がそれぞれ等しい合同条件'; sectionId = 'a75300e49184430eadbe7dd98718642b'; existingOverviewId = '2869e32d03f2465e881ef8096069d188'
        lessons = @(
            (L '三辺が等しいと三角形が決まることを確かめよう' 'concept' 'congruence.sss.meaning' '三辺の長さが決まると三角形の形と大きさが一つに決まることを説明する' '三辺の合計が同じなら合同とする' 7),
            (L 'コンパスの円からSSSをイメージしよう' 'concept' 'congruence.sss.construct' '二つの円の交点で第三頂点が決まる構成からSSSを理解する' '辺の長さが三角形を作れる条件を無視する' 8),
            (L '図から等しい三辺の組をそろえよう' 'basic' 'congruence.sss.identify' '対応を守りながら三組の等しい辺を抽出する' '同じ三辺を重複して数える' 7),
            (L 'SSSで合同を示す短い証明を書こう' 'procedure' 'congruence.sss.proof' '仮定から三組の辺の等しさを並べ、SSSで合同と結論する' '合同を先に結論し、辺の等しさを後から書く' 8),
            (L 'SSSを使える図か診断しよう' 'mastery' 'congruence.sss.diagnose' '三組そろっているかを確認し、不足条件を特定する' '二組の辺だけでSSSを使う' 7)
        )
    },
    [pscustomobject]@{
        title = '二辺とその間の角が等しい合同条件'; sectionId = '73be54b4fcf8437091ec2cb2f7e6ced9'; existingOverviewId = '77cd2ee31f114cb9892867c6f15006e9'
        lessons = @(
            (L '二辺とその間の角で三角形が決まる理由をつかもう' 'concept' 'congruence.sas.meaning' '二辺とその間の角が決まると三角形が一つに決まることを説明する' '二辺と任意の一角で合同とする' 7),
            (L '二辺の間にある角を見つけよう' 'basic' 'congruence.sas.included-angle' '指定された二辺が共有する頂点の角を、その間の角として選ぶ' '二辺のどちらかに接する角ならよいと考える' 7),
            (L '図からSASの三条件をそろえよう' 'basic' 'congruence.sas.identify' '二辺・その間の角の順に対応する三条件を抽出する' '角を先に選び、その両側の辺を確認しない' 7),
            (L 'SASで合同を示す証明を書こう' 'procedure' 'congruence.sas.proof' '仮定と共通辺などからSASの条件をそろえ、対応順で合同を書く' 'その間の角である理由を省略する' 8),
            (L 'SSAが合同条件でないことを見抜こう' 'misconception' 'congruence.sas.reject-ssa' '二辺とその間でない角では複数の三角形ができる場合を説明する' 'SASとSSAを同じ条件として扱う' 8)
        )
    },
    [pscustomobject]@{
        title = '一辺とその両端の角が等しい合同条件'; sectionId = '7d29011ad3514e24b905c19c30aed63b'; existingOverviewId = 'dc6ba22e1e6f4546aefdb13a8d0d9016'
        lessons = @(
            (L '一辺とその両端の角で三角形が決まる理由をつかもう' 'concept' 'congruence.asa.meaning' '一辺とその両端の角が決まると残る頂点が一つに決まることを説明する' '一辺と任意の二角で無条件に合同とする' 7),
            (L '一辺の両端にある角を見つけよう' 'basic' 'congruence.asa.endpoint-angles' '指定された辺の二つの端点にある角を選ぶ' '辺から離れた角を条件に含める' 7),
            (L '図からASAの三条件をそろえよう' 'basic' 'congruence.asa.identify' '一辺とその両端の角を対応順に抽出する' '等しい二角に挟まれていない辺をそのまま使う' 7),
            (L 'ASAで合同を示す証明を書こう' 'procedure' 'congruence.asa.proof' '平行線の角や共通辺を根拠にASAの条件をそろえて合同を示す' '同位角・錯角の位置を確認せず等しいとする' 8),
            (L '三つの合同条件を比較しよう' 'mastery' 'congruence.conditions.compare' 'SSS・SAS・ASAを、必要な辺と角の位置で区別する' '数が三つそろえばどの組合せでも合同とする' 8)
        )
    },
    [pscustomobject]@{
        title = '合同条件を選び、合同な三角形を見つけよう'; sectionId = 'b1c897ea9d31443c8119b05983b77fa8'; existingOverviewId = 'dcce738c4d8647f3babdbce9d685bb74'
        lessons = @(
            (L '仮定と図の印から与条件を抜き出そう' 'prerequisite' 'congruence.given.extract' '仮定・図の印・既知の性質を分けて合同に使える条件を列挙する' '図の見た目を与条件として使う' 7),
            (L '共通な辺と角を合同条件に加えよう' 'basic' 'congruence.common-element' '二三角形が共有する辺や角を等しい条件として言葉にする' '共通部分は書かなくても伝わると考える' 7),
            (L '対頂角・平行線から等しい角を作ろう' 'application' 'congruence.angle-evidence' '対頂角や平行線の性質から合同条件に必要な角の等しさを導く' '根拠を書かず角が等しいとする' 8),
            (L 'そろった条件から合同条件を選ぼう' 'application' 'congruence.condition.select' '辺と角の位置を照合し、SSS・SAS・ASAから使える条件を選ぶ' '先に使いたい条件を決めて図を合わせる' 8),
            (L '図の中から合同な三角形を見つけよう' 'mastery' 'congruence.triangle.find' '複数の三角形から対応と条件がそろう組を見つけ、合同を正しい順で書く' '形が似て見える三角形を選ぶ' 9)
        )
    },
    [pscustomobject]@{
        title = '根拠をつないで説明しよう・マスターチェック'; sectionId = '17bc15d1ec7841389b5eb8eee7076406'; existingOverviewId = '7141ddacca8a4a2796ce69c9e0421a66'
        lessons = @(
            (L '仮定・結論・根拠を分けて読もう' 'overview' 'proof.parts.identify' '証明問題を仮定・結論・使える根拠に分解する' '証明すべき結論を途中の根拠として使う' 7),
            (L '角や辺が等しい理由を一文で書こう' 'basic' 'proof.reason.write' '仮定・対頂角・平行線・共通部分を根拠付きの等式として書く' '理由を「見れば分かる」で済ませる' 7),
            (L '合同までの根拠を順番につなごう' 'procedure' 'proof.chain.build' '与条件から三条件、合同条件、結論へ論理を順に接続する' '合同後に分かる性質を合同前の条件として使う' 9),
            (L '誤った証明の最初の行を直そう' 'misconception' 'proof.error.diagnose' '循環論法・対応違い・根拠不足の最初の誤りを特定して修正する' '最後の結論だけを直せばよいと考える' 8),
            (L '平行線と合同を入試形式で総合確認しよう' 'mastery' 'parallel-congruence.mastery' '角の関係から合同条件を選び、根拠付きで証明して弱点の戻り先を決める' '計算正答だけで証明技能も身についたと判断する' 10)
        )
    }
)

$sections = @()
for ($sectionIndex = 0; $sectionIndex -lt $sectionSpecs.Count; $sectionIndex++) {
    $spec = $sectionSpecs[$sectionIndex]
    $lessons = @()
    for ($lessonIndex = 0; $lessonIndex -lt $spec.lessons.Count; $lessonIndex++) {
        $source = $spec.lessons[$lessonIndex]
        $previousSkill = if ($lessonIndex -gt 0) {
            $spec.lessons[$lessonIndex - 1].primarySkillId
        } elseif ($sectionIndex -gt 0) {
            $sectionSpecs[$sectionIndex - 1].lessons[-1].primarySkillId
        } else {
            'plane-geometry.angle-and-symbol-basics'
        }
        $nextSkill = if ($lessonIndex -lt $spec.lessons.Count - 1) {
            $spec.lessons[$lessonIndex + 1].primarySkillId
        } elseif ($sectionIndex -lt $sectionSpecs.Count - 1) {
            $sectionSpecs[$sectionIndex + 1].lessons[0].primarySkillId
        } else {
            'triangle-quadrilateral-proof.entry'
        }
        $slug = ('pc-{0:d2}-{1:d2}' -f ($sectionIndex + 1), ($lessonIndex + 1))
        $lessons += [ordered]@{
            title = $source.title
            role = $source.role
            primarySkillId = $source.primarySkillId
            positionInSection = $lessonIndex + 1
            lessonCountInSection = $spec.lessons.Count
            receivesSkillIds = @($previousSkill, $source.primarySkillId)
            handsOffSkillIds = @($nextSkill)
            inScope = @($source.goal)
            outOfScope = @("次の中心技能「$nextSkill」の詳しい学習")
            estimatedMinutes = $source.estimatedMinutes
            misconceptionIds = @($source.misconception)
            quiz = @(
                [ordered]@{ questionId = "$slug-direct"; type = 'direct'; skillId = $source.primarySkillId },
                [ordered]@{ questionId = "$slug-misconception"; type = 'misconception'; skillId = $source.primarySkillId }
            )
        }
    }
    $sections += [ordered]@{
        title = $spec.title
        sectionId = $spec.sectionId
        existingOverviewId = $spec.existingOverviewId
        lessons = $lessons
    }
}

$plan = [ordered]@{
    courseTitle = $courseTitle
    courseId = $courseId
    organizationId = 'a461577a-3410-4c98-b1d5-db729f3444a1'
    audience = '中学2年生・高校受験基礎〜標準'
    learningModes = @('conceptual understanding', 'procedure and fluency', 'reading and interpretation', 'integrated application')
    sections = $sections
}

$outputPath = Join-Path $PSScriptRoot 'lesson-plan-parallel-congruence.json'
$plan | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $outputPath -Encoding utf8
Write-Output "Wrote $outputPath with $($sections.Count) sections and $(($sections.lessons | Measure-Object).Count) lessons."
