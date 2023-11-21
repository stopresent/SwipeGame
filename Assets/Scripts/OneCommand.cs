using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class OneCommand : MonoBehaviour
{
    #region �±׿� ���� �Լ�ȣ��
    private void Awake()
    {
        if (CompareTag("GameManager")) Awake_GM();
    }

    private void Update()
    {
        if (CompareTag("GameManager")) Update_GM();
    }

    private void FixedUpdate()
    {
        if (CompareTag("GameManager")) FixedUpdate_GM();
    }

    private void Start()
    {
        if (CompareTag("Ball")) Start_BALL();
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (CompareTag("Ball")) StartCoroutine(OnCollisionEnter2D_BALL(col));
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (CompareTag("Ball")) StartCoroutine(OnTriggerEnter2D_BALL(col));
    }
    #endregion

    #region GameManager.Cs
    [Header("GameManagerValue")]
    public float groundY = -55.489f;
    public GameObject P_Ball, P_GreenOrb, P_Block, P_ParticleBlue, P_ParticleGreen, P_ParticleRed;
    public GameObject BallPreview, Arrow, GameOverPanel, BallCountTextObj, BallPlusTextObj;
    public Transform GreenBallGroup, BlockGroup, BallGroup;
    public LineRenderer MouseLR, BallLR;
    public Text BestScoreText, ScoreText, BallCountText, BallPlusText, FinalScoreText, NewRecordText;
    public Color[] blockColor;
    public Color greenColor;
    public AudioSource S_GameOver, S_GreenOrb, S_Plus;
    public AudioSource[] S_Block;
    public Quaternion QI = Quaternion.identity;
    public bool shotTrigger, shotable;
    public Vector3 veryFirstPos;

    Vector3 firstPos, secondPos, gap;
    int score, timerCount, launchIndex;
    bool timerStart, isDie, isNewRecord, isBlockMoving;
    float timeDelay;

    #region ����
    void Awake_GM()
    {
        // 9:16 �����ػ� ī�޶�
        Camera camera = Camera.main;
        Rect rect = camera.rect;
        float scaleheight = ((float)Screen.width / Screen.height) / ((float)9 / 16); // (���� / ����)
        float scalewidth = 1f / scaleheight;
        if (scaleheight < 1)
        {
            rect.height = scaleheight;
            rect.y = (1f - scaleheight) / 2f;
        }
        else
        {
            rect.width = scalewidth;
            rect.x = (1f - scalewidth) / 2f;
        }
        camera.rect = rect;


        // ����
        BlockGenerator();
        BestScoreText.text = "�ְ��� : " + PlayerPrefs.GetInt("BestScore").ToString();
    }

    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    public void VeryFirstPosSet(Vector3 pos) { if (veryFirstPos == Vector3.zero) veryFirstPos = pos; }
    #endregion

    #region ��
    void BlockGenerator()
    {
        // ����
        ScoreText.text = "�������� : " + (++score).ToString();
        if (PlayerPrefs.GetInt("BestScore", 0) < score)
        {
            PlayerPrefs.SetInt("BestScore", score);
            BestScoreText.text = "�ְ��� : " + PlayerPrefs.GetInt("BestScore").ToString();
            BestScoreText.color = greenColor;
            isNewRecord = true;
        }

        // ������ ���� �����簳�� ���ϱ�
        int count;
        int randBlock = Random.Range(0, 24);
        if (score <= 10) count = randBlock < 16 ? 1 : 2;
        else if (score <= 20) count = randBlock < 8 ? 1 : (randBlock < 16 ? 2 : 3);
        else if (score <= 40) count = randBlock < 9 ? 2 : (randBlock < 18 ? 3 : 4);
        else count = randBlock < 9 ? 2 : (randBlock < 16 ? 3 : (randBlock < 20 ? 4 : 5));

        // ������ǥ�� ��, �ʷϱ� ����
        List<Vector3> SpawnLIst = new List<Vector3>();
        for (int i = 0; i < 6; i++) SpawnLIst.Add(new Vector3(-46.7f + i * 18.68f, 51.2f, 0));

        for (int i = 0; i < count; i++)
        {
            int rand = Random.Range(0, SpawnLIst.Count);

            Transform TR = Instantiate(P_Block, SpawnLIst[rand], QI).transform;
            TR.SetParent(BlockGroup);
            TR.GetChild(0).GetComponentInChildren<Text>().text = score.ToString();

            SpawnLIst.RemoveAt(rand);
        }
        Instantiate(P_GreenOrb, SpawnLIst[Random.Range(0, SpawnLIst.Count)], QI).transform.SetParent(BlockGroup);

        // �� ������
        isBlockMoving = true;
        for (int i = 0; i < BlockGroup.childCount; i++) StartCoroutine(BlockMoveDown(BlockGroup.GetChild(i)));
    }


    IEnumerator BlockMoveDown(Transform TR)
    {
        yield return new WaitForSeconds(0.2f);
        Vector3 targetPos = TR.position + new Vector3(0, -12.8f, 0);
        BlockColorChange();


        // �����̸� ���ӿ��� Ʈ����, �ݶ��̴� ��Ȱ��ȭ
        if(targetPos.y < -50)
        {
            if (TR.CompareTag("Block")) isDie = true;
            for (int i = 0; i < BallGroup.childCount; i++)
                BallGroup.GetChild(i).GetComponent<CircleCollider2D>().enabled = false;
        }

        // 0.3�ʰ� �� �̵�
        float TT = 1.5f;
        while (true)
        {
            yield return null; TT -= Time.deltaTime * 1.5f;
            TR.position = Vector3.MoveTowards(TR.position, targetPos + new Vector3(0, -6, 0), TT);
            if (TR.position == targetPos + new Vector3(0, -6, 0)) break;
        }
        TT = 0.9f;
        while (true)
        {
            yield return null; TT -= Time.deltaTime;
            TR.position = Vector3.MoveTowards(TR.position, targetPos, TT);
            if (TR.position == targetPos) break;
        }
        isBlockMoving = false;

        // �̵��ǰ� �� �� �����̸� ���̸� ���ӿ���, �ʷϱ��� �ı�
        if(targetPos.y < -50)
        {
            if (TR.CompareTag("Block"))
            {
                for (int i = 0; i < BallGroup.childCount; i++)
                    Destroy(BallGroup.GetChild(i).gameObject);
                Destroy(Instantiate(P_ParticleBlue, veryFirstPos, QI), 1);

                BallCountTextObj.SetActive(false);
                BallPlusTextObj.SetActive(false);
                BestScoreText.gameObject.SetActive(false);
                ScoreText.gameObject.SetActive(false);

                GameOverPanel.SetActive(true);
                FinalScoreText.text = "�������� : " + score.ToString();
                if (isNewRecord) NewRecordText.gameObject.SetActive(true);

                Camera.main.GetComponent<Animator>().SetTrigger("shake");
                S_GameOver.Play();
            }
            else
            {
                Destroy(TR.gameObject);
                Destroy(Instantiate(P_ParticleGreen, TR.position, QI), 1);

                for (int i = 0; i < BallGroup.childCount; i++)
                    BallGroup.GetChild(i).GetComponent<CircleCollider2D>().enabled = true;
            }
        }
    }

    public void BlockColorChange()
    {
        // ���ؽ�Ʈ / ���ھ 7����ؼ� ���� ĥ��
        for (int i = 0; i < BlockGroup.childCount; i++)
        {
            if (BlockGroup.GetChild(i).CompareTag("Block"))
            {
                float per = int.Parse(BlockGroup.GetChild(i).GetChild(0).GetComponentInChildren<Text>().text) / (float)score;
                Color curColor;
                if (per <= 0.1428f) curColor = blockColor[6];
                else if (per <= 0.2856f) curColor = blockColor[5];
                else if (per <= 0.4284f) curColor = blockColor[4];
                else if (per <= 0.5172f) curColor = blockColor[3];
                else if (per <= 0.714f) curColor = blockColor[2];
                else if (per <= 0.8568f) curColor = blockColor[1];
                else curColor = blockColor[0];
                BlockGroup.GetChild(i).GetComponent<SpriteRenderer>().color = curColor;
            }
        }
    }



    #endregion

    void Update_GM()
    {
        if (isDie) return;

        // ���콺 ù��° ��ǥ
        if (Input.GetMouseButtonDown(0))
            firstPos = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0, 0, 10);


        // ��� �������� ������ �� �� ����
        shotable = true;
        for (int i = 0; i < BallGroup.childCount; i++)
            if (BallGroup.GetChild(i).GetComponent<OneCommand>().isMoving) shotable = false;
        if (isBlockMoving) shotable = false;

        if (!shotable) return;


        // ��� ���� �ٴڿ� �ε����� �� �� ����
        if (shotTrigger && shotable)
        {
            shotTrigger = false;
            BlockGenerator();
            timeDelay = 0;

            StartCoroutine(BallCountTextShow(GreenBallGroup.childCount));
            for (int i = 0; i < GreenBallGroup.childCount; i++) StartCoroutine(GreenBallMove(GreenBallGroup.GetChild(i)));
        }

        timeDelay += Time.deltaTime;
        if (timeDelay < 0.1f) return; // 0.1�� �����̷� �ʹ� ������ �� ���� ������ ���� ���� ����

        bool isMouse = Input.GetMouseButton(0);
        if (isMouse)
        {
            // ���̰�
            secondPos = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0, 0, 10);
            if ((secondPos - firstPos).magnitude < 1) return;
            gap = (secondPos - firstPos).normalized;
            gap = new Vector3(gap.y >= 0 ? gap.x : gap.x >= 0 ? 1 : -1, Mathf.Clamp(gap.y, 0.2f, 1), 0);

            // ȭ��ǥ, �� �̸�����
            Arrow.transform.position = veryFirstPos;
            Arrow.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(gap.y, gap.x) * Mathf.Rad2Deg);
            BallPreview.transform.position =
                Physics2D.CircleCast(new Vector2(Mathf.Clamp(veryFirstPos.x, -54, 54), groundY), 1.7f, gap, 10200, 1 << LayerMask.NameToLayer("Wall") | 1 << LayerMask.NameToLayer("Block")).centroid;

            RaycastHit2D hit = Physics2D.Raycast(veryFirstPos, gap, 10200, 1 << LayerMask.NameToLayer("Wall"));

            // ����
            MouseLR.SetPosition(0, firstPos);
            MouseLR.SetPosition(1, secondPos);
            BallLR.SetPosition(0, veryFirstPos);
            BallLR.SetPosition(1, (Vector3)hit.point - gap * 1.5f);
        }
        BallPreview.SetActive(isMouse);
        Arrow.SetActive(isMouse);

        if (Input.GetMouseButtonUp(0))
        {
            if ((secondPos - firstPos).magnitude < 1) return;

            // ���� �ʱ�ȭ
            MouseLR.SetPosition(0, Vector3.zero);
            MouseLR.SetPosition(1, Vector3.zero);
            BallLR.SetPosition(0, Vector3.zero);
            BallLR.SetPosition(1, Vector3.zero);

            timerStart = true;
            veryFirstPos = Vector3.zero;
            firstPos = Vector3.zero;
        }
    }


    IEnumerator BallCountTextShow(int greenBallCount)
    {
        // �ʷϰ� �������� ���� �� ���� �����ֱ�
        BallCountTextObj.transform.position = new Vector3(Mathf.Clamp(veryFirstPos.x, -49.9f, 49.9f), -65, 0);
        BallCountText.text = "x" + BallGroup.childCount.ToString();

        yield return new WaitForSeconds(0.17f);

        if (BallGroup.childCount > score) Destroy(BallGroup.GetChild(BallGroup.childCount - 1).gameObject);
        BallCountText.text = "x" + BallGroup.childCount.ToString();

        // �ٴڿ� ������ �ʷϰ� +�� ǥ���ϱ�
        if (greenBallCount != 0)
        {
            BallPlusTextObj.SetActive(true);
            BallPlusTextObj.transform.position = new Vector3(Mathf.Clamp(veryFirstPos.x, -49.9f, 49.9f), -47, 0);
            BallPlusText.text = "+" + greenBallCount.ToString();
            S_Plus.Play();

            yield return new WaitForSeconds(0.5f);

            BallPlusTextObj.SetActive(false);
        }
    }


    IEnumerator GreenBallMove(Transform TR)
    {
        // �ٴڿ� ������ �ʷϰ� ������ǥ�� 0.17�ʰ� �̵�
        Instantiate(P_Ball, veryFirstPos, QI).transform.SetParent(BallGroup);
        float speed = (TR.position - veryFirstPos).magnitude / 12f;
        while (true)
        {
            yield return null;
            TR.position = Vector3.MoveTowards(TR.position, veryFirstPos, speed);
            if (TR.position == veryFirstPos) { Destroy(TR.gameObject); yield break; }
        }
    }


    private void FixedUpdate_GM()
    {
        // 0.06�� �������� �� �߻�
        if (timerStart && ++timerCount == 3)
        {
            timerCount = 0;
            BallGroup.GetChild(launchIndex++).GetComponent<OneCommand>().Launch(gap);
            BallCountText.text = "x" + (BallGroup.childCount - launchIndex).ToString();
            if (launchIndex == BallGroup.childCount)
            {
                timerStart = false;
                launchIndex = 0;
                BallCountText.text = "";
            }
        }
    }

    #endregion

    #region BallScript.Cs
    [Header("BallScriptValue")]
    public GameObject P_GreenBall;
    public Rigidbody2D rigid;
    public bool isMoving;

    OneCommand GM;

    void Start_BALL() => GM = GameObject.FindWithTag("GameManager").GetComponent<OneCommand>();

    public void Launch(Vector3 pos)
    {
        GM.shotTrigger = true;
        isMoving = true;
        rigid.AddForce(pos * 7000);
    }

    IEnumerator OnCollisionEnter2D_BALL(Collision2D col)
    {
        GameObject Col = col.gameObject;
        Physics2D.IgnoreLayerCollision(2, 2);

        // ���η� �����ϰ�� �Ʒ��� ����
        Vector2 pos = rigid.velocity.normalized;
        if (pos.magnitude != 0 && pos.y < 0.15f && pos.y > -0.15f)
        {
            rigid.velocity = Vector2.zero;
            rigid.AddForce(new Vector2(pos.x > 0 ? 1 : -1, -0.2f).normalized * 7000);
        }

        // �ٴ��浹�� ������ǥ�� �̵�
        if (Col.CompareTag("Ground"))
        {
            rigid.velocity = Vector2.zero;
            transform.position = new Vector2(col.contacts[0].point.x, GM.groundY);
            GM.VeryFirstPosSet(transform.position);

            while (true)
            {
                yield return null;
                transform.position = Vector3.MoveTowards(transform.position, GM.veryFirstPos, 4);
                if (transform.position == GM.veryFirstPos) { isMoving = false; yield break; }
            }
        }

        // ���浹�� ������ 1�� ���̴� 0�̵Ǹ� �μ�
        if (Col.CompareTag("Block"))
        {
            Text BlockText = Col.transform.GetChild(0).GetComponentInChildren<Text>();
            int blockValue = int.Parse(BlockText.text) - 1;
            GM.BlockColorChange();


            for (int i = 0; i < GM.S_Block.Length; i++)
            {
                if (GM.S_Block[i].isPlaying) continue;
                else { GM.S_Block[i].Play(); break; }
            }

            if (blockValue > 0)
            {
                BlockText.text = blockValue.ToString();
                Col.GetComponent<Animator>().SetTrigger("shock");
            }
            else
            {
                Destroy(Col);
                Destroy(Instantiate(GM.P_ParticleRed, col.transform.position, Quaternion.identity), 1);
            }
        }
    }


    IEnumerator OnTriggerEnter2D_BALL(Collider2D col)
    {
        // �ʷϱ� �浹�� �ʷϰ� �����ؼ� �Ʒ��� ������
        if (col.gameObject.CompareTag("GreenOrb"))
        {
            Destroy(col.gameObject);
            Destroy(Instantiate(GM.P_ParticleGreen, col.transform.position, GM.QI), 1);

            GM.S_GreenOrb.Play();
            Transform TR = Instantiate(P_GreenBall, col.transform.position, GM.QI).transform;
            TR.SetParent(GameObject.Find("GreenBallGroup").transform);
            Vector3 targetPos = new Vector3(TR.position.x, GM.groundY, 0);
            while (true)
            {
                yield return null;
                TR.position = Vector3.MoveTowards(TR.position, targetPos, 2.5f);
                if (TR.position == targetPos) yield break;
            }
        }
    }
    #endregion
}
